namespace AILA.Application.Features.Authentication.Dtos
{
    // ----- Request bodies (Api → Application) -----

    /// <summary>Body của <c>POST /api/auth/password-reset/request</c>.</summary>
    public class RequestPasswordResetRequestDto
    {
        public string Email { get; set; } = string.Empty;
    }

    /// <summary>Body của <c>POST /api/auth/password-reset/verify</c>.</summary>
    public class VerifyPasswordResetOtpRequestDto
    {
        public string Email { get; set; } = string.Empty;
        public string Otp { get; set; } = string.Empty;
    }

    /// <summary>Body của <c>POST /api/auth/password-reset/confirm</c>.</summary>
    public class ConfirmPasswordResetRequestDto
    {
        public string ResetToken { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    // ----- Response bodies -----

    /// <summary>
    /// AC-1: response trung tính — cùng một nội dung dù email có tồn tại hay không,
    /// để không lộ thông tin tài khoản (chống user enumeration).
    /// </summary>
    public class RequestPasswordResetResponseDto
    {
        public string Message { get; set; } =
            "Nếu email hợp lệ, chúng tôi đã gửi mã OTP tới hộp thư của bạn.";

        /// <summary>Số giây OTP còn hiệu lực, để UI hiển thị đồng hồ đếm ngược.</summary>
        public int OtpExpiresInSeconds { get; set; }
    }

    /// <summary>AC-4: OTP đúng → cấp reset token dùng một lần, TTL ngắn.</summary>
    public class VerifyPasswordResetOtpResponseDto
    {
        public string ResetToken { get; set; } = string.Empty;
        public int ExpiresInSeconds { get; set; }
    }
}
