using AILA.Application.Common.Exceptions;
using AILA.Application.Common.Helpers;
using AILA.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Models;
using Shared.Wrappers;

namespace AILA.Application.Features.Authentication.Commands.ConfirmPasswordReset
{
    /// <summary>
    /// AC-6/AC-7/AC-8: đổi password bằng reset token.
    ///
    /// Thứ tự thao tác có chủ đích: <b>validate xong hết rồi mới consume token</b>.
    /// Nếu consume trước, một lần nhập password sai policy sẽ đốt token oan và bắt người
    /// dùng xin lại OTP (vi phạm AC-7). Bước consume dùng GETDEL atomic nên vẫn chỉ đúng
    /// một caller thắng khi có hai request đồng thời (EDGE-10, EDGE-12).
    /// </summary>
    public class ConfirmPasswordResetCommandHandler
        : IRequestHandler<ConfirmPasswordResetCommand, ResponseDto<object>>
    {
        private readonly IUnitOfWork _uow;
        private readonly IPasswordResetStore _store;
        private readonly IPasswordHasher _passwordHasher;
        private readonly PasswordResetSettings _settings;
        private readonly ILogger<ConfirmPasswordResetCommandHandler> _logger;

        public ConfirmPasswordResetCommandHandler(
            IUnitOfWork uow,
            IPasswordResetStore store,
            IPasswordHasher passwordHasher,
            IOptions<PasswordResetSettings> settings,
            ILogger<ConfirmPasswordResetCommandHandler> logger)
        {
            _uow = uow;
            _store = store;
            _passwordHasher = passwordHasher;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<ResponseDto<object>> Handle(
            ConfirmPasswordResetCommand request,
            CancellationToken cancellationToken)
        {
            // --- EDGE-01: input rỗng/null ---
            if (string.IsNullOrWhiteSpace(request.ResetToken))
                return ResponseDto<object>.FailResult(
                    PasswordResetErrorCodes.ValidationError,
                    "Reset token không được để trống.");

            // --- AC-7/AC-9/EDGE-08: gom mọi vi phạm password vào một lần trả về.
            //     Token chưa bị đụng tới ở bước này. ---
            var violations = PasswordPolicy.Validate(request.NewPassword).ToList();

            if (!string.Equals(request.NewPassword, request.ConfirmPassword, StringComparison.Ordinal))
                violations.Add("Mật khẩu xác nhận không khớp với mật khẩu mới.");

            if (violations.Count > 0)
                return ResponseDto<object>.FailResult(
                    PasswordResetErrorCodes.InvalidPassword,
                    string.Join(" ", violations));

            var resetToken = request.ResetToken.Trim();

            try
            {
                // --- Đọc token mà chưa tiêu thụ, để còn kiểm tra tiếp EDGE-09 ---
                var peekedUserId = await _store.PeekResetTokenAsync(resetToken, cancellationToken);
                if (peekedUserId is null)
                    return InvalidToken();

                var user = await _uow.Users.GetByIdAsync(peekedUserId.Value);
                if (user is null || !user.IsActive)
                {
                    // Token trỏ tới tài khoản không còn dùng được — dọn luôn cho sạch.
                    await _store.ConsumeResetTokenAsync(resetToken, cancellationToken);
                    _logger.LogWarning(
                        "Reset token trỏ tới tài khoản không khả dụng. userId={UserId}", peekedUserId);
                    return InvalidToken();
                }

                // --- EDGE-09: từ chối password mới trùng password hiện tại, token vẫn giữ nguyên ---
                if (_settings.RejectPasswordSameAsCurrent
                    && user.PasswordHash is not null
                    && _passwordHasher.Verify(request.NewPassword, user.PasswordHash))
                {
                    return ResponseDto<object>.FailResult(
                        PasswordResetErrorCodes.PasswordReused,
                        "Mật khẩu mới không được trùng với mật khẩu hiện tại.");
                }

                // --- AC-6/AC-8/EDGE-10: GETDEL atomic. Ai lấy được userId thì người đó
                //     được đổi password; request thứ hai với cùng token nhận null. ---
                var consumedUserId = await _store.ConsumeResetTokenAsync(resetToken, cancellationToken);
                if (consumedUserId is null || consumedUserId.Value != user.Id)
                    return InvalidToken();

                // --- BR-03: hash một chiều (BCrypt), không bao giờ encrypt hai chiều ---
                user.UpdatePassword(_passwordHasher.HashPassword(request.NewPassword));
                await _uow.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Đổi password qua luồng reset thành công. email={Email}",
                    EmailHelper.Mask(user.Email));

                return ResponseDto<object>.SuccessResult(null!);
            }
            catch (PasswordResetStoreUnavailableException ex)
            {
                // EDGE-13: không đổi password khi không xác thực được token.
                _logger.LogError(ex, "Store của luồng password reset không sẵn sàng khi confirm.");

                return ResponseDto<object>.FailResult(
                    PasswordResetErrorCodes.ServiceUnavailable,
                    "Hệ thống đang bận. Vui lòng thử lại sau ít phút.");
            }
        }

        private static ResponseDto<object> InvalidToken()
            => ResponseDto<object>.FailResult(
                PasswordResetErrorCodes.InvalidOrExpiredToken,
                "Yêu cầu đặt lại mật khẩu không hợp lệ hoặc đã hết hạn. Vui lòng thực hiện lại từ đầu.");
    }
}
