using AILA.Application.Common.Exceptions;
using AILA.Application.Common.Helpers;
using AILA.Application.Common.Interfaces;
using AILA.Application.Features.Authentication.Dtos;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Models;
using Shared.Wrappers;

namespace AILA.Application.Features.Authentication.Commands.VerifyPasswordResetOtp
{
    /// <summary>
    /// AC-4/AC-5: OTP đúng → cấp reset token và xoá OTP (chống replay);
    /// OTP sai/hết hạn → 400 với mã chung, đồng thời đếm số lần sai để chặn brute-force.
    /// </summary>
    public class VerifyPasswordResetOtpCommandHandler
        : IRequestHandler<VerifyPasswordResetOtpCommand, ResponseDto<VerifyPasswordResetOtpResponseDto>>
    {
        private readonly IUnitOfWork _uow;
        private readonly IPasswordResetStore _store;
        private readonly IOtpService _otpService;
        private readonly PasswordResetSettings _settings;
        private readonly ILogger<VerifyPasswordResetOtpCommandHandler> _logger;

        public VerifyPasswordResetOtpCommandHandler(
            IUnitOfWork uow,
            IPasswordResetStore store,
            IOtpService otpService,
            IOptions<PasswordResetSettings> settings,
            ILogger<VerifyPasswordResetOtpCommandHandler> logger)
        {
            _uow = uow;
            _store = store;
            _otpService = otpService;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<ResponseDto<VerifyPasswordResetOtpResponseDto>> Handle(
            VerifyPasswordResetOtpCommand request,
            CancellationToken cancellationToken)
        {
            // --- EDGE-01: input rỗng/null ---
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Otp))
                return Fail(
                    PasswordResetErrorCodes.ValidationError,
                    "Email và mã OTP không được để trống.");

            var email = EmailHelper.Normalize(request.Email);
            var maskedEmail = EmailHelper.Mask(email);
            var otp = request.Otp.Trim();

            if (!EmailHelper.IsValidFormat(email))
                return Fail(
                    PasswordResetErrorCodes.InvalidEmailFormat,
                    "Email không đúng định dạng.");

            try
            {
                // --- AC-5 / EDGE-05 / EDGE-07: hết hạn (TTL tự xoá) hoặc đã consume ⇒ không còn bản ghi ---
                var entry = await _store.GetOtpAsync(email, cancellationToken);
                if (entry is null)
                {
                    _logger.LogInformation(
                        "Verify OTP thất bại — không còn OTP active. email={Email}", maskedEmail);
                    return InvalidOtp();
                }

                // --- So sánh constant-time (chống timing attack) ---
                if (!_otpService.VerifyOtp(email, otp, entry.OtpHash))
                {
                    // --- EDGE-06: đếm số lần sai, vượt ngưỡng thì huỷ OTP, buộc xin mã mới ---
                    var attempts = await _store.IncrementOtpAttemptAsync(email, cancellationToken);

                    if (attempts >= _settings.MaxVerifyAttempts)
                    {
                        await _store.DeleteOtpAsync(email, cancellationToken);
                        _logger.LogWarning(
                            "OTP bị huỷ do vượt ngưỡng thử sai. email={Email}, attempts={Attempts}",
                            maskedEmail, attempts);
                    }
                    else
                    {
                        _logger.LogInformation(
                            "Verify OTP sai. email={Email}, attempts={Attempts}", maskedEmail, attempts);
                    }

                    return InvalidOtp();
                }

                // --- AC-4: OTP hợp lệ ⇒ xoá ngay để chống replay (one-time, BR-02) ---
                await _store.DeleteOtpAsync(email, cancellationToken);

                var user = await _uow.Users.GetByEmailAsync(email);
                if (user is null || !user.IsActive)
                {
                    // Tài khoản biến mất/bị khoá giữa chừng — trả cùng lỗi chung, không tiết lộ.
                    _logger.LogWarning(
                        "OTP đúng nhưng tài khoản không còn khả dụng. email={Email}", maskedEmail);
                    return InvalidOtp();
                }

                // --- Cấp reset token opaque (CSPRNG), TTL ngắn, dùng một lần ---
                var resetToken = _otpService.GenerateResetToken();
                await _store.SaveResetTokenAsync(resetToken, user.Id, cancellationToken);

                _logger.LogInformation("Verify OTP thành công, đã cấp reset token. email={Email}", maskedEmail);

                return ResponseDto<VerifyPasswordResetOtpResponseDto>.SuccessResult(
                    new VerifyPasswordResetOtpResponseDto
                    {
                        ResetToken = resetToken,
                        ExpiresInSeconds = _settings.ResetTokenTtlSeconds
                    });
            }
            catch (PasswordResetStoreUnavailableException ex)
            {
                // EDGE-13: không xác thực được OTP thì tuyệt đối không cho đi tiếp.
                _logger.LogError(ex,
                    "Store của luồng password reset không sẵn sàng khi verify OTP. email={Email}", maskedEmail);

                return Fail(
                    PasswordResetErrorCodes.ServiceUnavailable,
                    "Hệ thống đang bận. Vui lòng thử lại sau ít phút.");
            }
        }

        /// <summary>AF-02: một mã lỗi duy nhất cho mọi lý do, không tiết lộ chi tiết.</summary>
        private static ResponseDto<VerifyPasswordResetOtpResponseDto> InvalidOtp()
            => Fail(
                PasswordResetErrorCodes.InvalidOrExpiredOtp,
                "Mã OTP không đúng hoặc đã hết hạn.");

        private static ResponseDto<VerifyPasswordResetOtpResponseDto> Fail(string code, string message)
            => ResponseDto<VerifyPasswordResetOtpResponseDto>.FailResult(code, message);
    }
}
