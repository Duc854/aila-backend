using System.Diagnostics;
using AILA.Application.Common.Exceptions;
using AILA.Application.Common.Helpers;
using AILA.Application.Common.Interfaces;
using AILA.Application.Features.Authentication.Dtos;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Models;
using Shared.Wrappers;

namespace AILA.Application.Features.Authentication.Commands.RequestPasswordReset
{
    /// <summary>
    /// AC-1/AC-2/AC-3: sinh OTP, lưu hash vào store với TTL, đẩy email chứa OTP sang
    /// Email Service. Response luôn trung tính để không lộ email có tồn tại hay không.
    /// </summary>
    public class RequestPasswordResetCommandHandler
        : IRequestHandler<RequestPasswordResetCommand, ResponseDto<RequestPasswordResetResponseDto>>
    {
        private readonly IUnitOfWork _uow;
        private readonly IPasswordResetStore _store;
        private readonly IOtpService _otpService;
        private readonly IEmailSender _emailSender;
        private readonly PasswordResetSettings _settings;
        private readonly ILogger<RequestPasswordResetCommandHandler> _logger;

        public RequestPasswordResetCommandHandler(
            IUnitOfWork uow,
            IPasswordResetStore store,
            IOtpService otpService,
            IEmailSender emailSender,
            IOptions<PasswordResetSettings> settings,
            ILogger<RequestPasswordResetCommandHandler> logger)
        {
            _uow = uow;
            _store = store;
            _otpService = otpService;
            _emailSender = emailSender;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<ResponseDto<RequestPasswordResetResponseDto>> Handle(
            RequestPasswordResetCommand request,
            CancellationToken cancellationToken)
        {
            // Đo từ đầu để đệm thời gian phản hồi về một mức cố định (chống enumeration qua timing).
            var stopwatch = Stopwatch.StartNew();

            // --- EDGE-01: input rỗng/null ---
            if (string.IsNullOrWhiteSpace(request.Email))
                return ResponseDto<RequestPasswordResetResponseDto>.FailResult(
                    PasswordResetErrorCodes.ValidationError,
                    "Email không được để trống.");

            // --- EDGE-14: chuẩn hoá trước khi băm key ---
            var email = EmailHelper.Normalize(request.Email);
            var maskedEmail = EmailHelper.Mask(email);

            // --- EDGE-03: chặn định dạng sai trước khi chạm Redis/DB ---
            if (!EmailHelper.IsValidFormat(email))
                return ResponseDto<RequestPasswordResetResponseDto>.FailResult(
                    PasswordResetErrorCodes.InvalidEmailFormat,
                    "Email không đúng định dạng.");

            try
            {
                // --- EDGE-04: rate limit (đếm ở Redis nên đồng nhất giữa nhiều instance) ---
                if (!string.IsNullOrWhiteSpace(request.IpAddress))
                {
                    var ipCount = await _store.IncrementRateLimitAsync("ip", request.IpAddress!, cancellationToken);
                    if (ipCount > _settings.MaxOtpRequestsPerIp)
                    {
                        _logger.LogWarning(
                            "Password reset rate limit hit theo IP. ip={Ip}, count={Count}",
                            request.IpAddress, ipCount);

                        return ResponseDto<RequestPasswordResetResponseDto>.FailResult(
                            PasswordResetErrorCodes.RateLimitExceeded,
                            "Bạn đã yêu cầu quá nhiều lần. Vui lòng thử lại sau.");
                    }
                }

                var emailCount = await _store.IncrementRateLimitAsync("email", email, cancellationToken);
                if (emailCount > _settings.MaxOtpRequestsPerEmail)
                {
                    _logger.LogWarning(
                        "Password reset rate limit hit theo email. email={Email}, count={Count}",
                        maskedEmail, emailCount);

                    return ResponseDto<RequestPasswordResetResponseDto>.FailResult(
                        PasswordResetErrorCodes.RateLimitExceeded,
                        "Bạn đã yêu cầu quá nhiều lần. Vui lòng thử lại sau.");
                }

                // --- AF-01 / EDGE-02: email không tồn tại (hoặc bị vô hiệu hoá) thì
                //     không sinh OTP, nhưng response vẫn y hệt trường hợp thành công. ---
                var user = await _uow.Users.GetByEmailAsync(email);

                if (user is null || !user.IsActive)
                {
                    _logger.LogInformation(
                        "Password reset requested cho email không khả dụng — bỏ qua im lặng. email={Email}",
                        maskedEmail);

                    await PadResponseTimeAsync(stopwatch, cancellationToken);
                    return NeutralResult();
                }

                // --- AC-2: OTP 6 chữ số (CSPRNG), lưu hash + TTL vào store ---
                // --- AC-3/BR-02: SaveOtpAsync ghi đè bản ghi cũ ⇒ OTP cũ tự vô hiệu ---
                var otp = _otpService.GenerateOtp();
                var otpHash = _otpService.HashOtp(email, otp);

                await _store.SaveOtpAsync(email, otpHash, cancellationToken);

                // --- Đẩy email sang Email Service (enqueue, không block response) ---
                await _emailSender.SendPasswordResetOtpAsync(
                    user.Email,
                    user.FullName,
                    otp,
                    _settings.OtpTtlSeconds / 60,
                    cancellationToken);

                _logger.LogInformation(
                    "Password reset OTP đã được sinh và đưa vào hàng đợi gửi. email={Email}",
                    maskedEmail);

                await PadResponseTimeAsync(stopwatch, cancellationToken);
                return NeutralResult();
            }
            catch (PasswordResetStoreUnavailableException ex)
            {
                // EDGE-13: không cho bypass reset khi store không sẵn sàng.
                _logger.LogError(ex,
                    "Store của luồng password reset không sẵn sàng khi xin OTP. email={Email}",
                    maskedEmail);

                return ResponseDto<RequestPasswordResetResponseDto>.FailResult(
                    PasswordResetErrorCodes.ServiceUnavailable,
                    "Hệ thống đang bận. Vui lòng thử lại sau ít phút.");
            }
        }

        private ResponseDto<RequestPasswordResetResponseDto> NeutralResult()
            => ResponseDto<RequestPasswordResetResponseDto>.SuccessResult(
                new RequestPasswordResetResponseDto
                {
                    OtpExpiresInSeconds = _settings.OtpTtlSeconds
                });

        /// <summary>
        /// Đệm thời gian phản hồi lên mức tối thiểu cấu hình được, để nhánh "email tồn tại"
        /// và "email không tồn tại" mất thời gian như nhau (NFR Security — chống enumeration).
        /// </summary>
        private async Task PadResponseTimeAsync(Stopwatch stopwatch, CancellationToken cancellationToken)
        {
            var remaining = _settings.MinRequestDurationMs - (int)stopwatch.ElapsedMilliseconds;
            if (remaining > 0)
                await Task.Delay(remaining, cancellationToken);
        }
    }
}
