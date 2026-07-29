namespace AILA.Application.Common.Interfaces
{
    /// <summary>
    /// Sinh và xác thực OTP / reset token cho luồng Reset Password (UC-08).
    /// Mọi giá trị bí mật đều sinh bằng CSPRNG; so sánh luôn constant-time.
    /// </summary>
    public interface IOtpService
    {
        /// <summary>Sinh OTP số, độ dài lấy từ cấu hình (mặc định 6 chữ số).</summary>
        string GenerateOtp();

        /// <summary>
        /// Băm OTP (HMAC, gắn với email) để lưu Redis — không bao giờ lưu OTP thô.
        /// </summary>
        string HashOtp(string normalizedEmail, string otp);

        /// <summary>
        /// So sánh constant-time giữa OTP người dùng nhập và hash đã lưu (chống timing attack).
        /// </summary>
        bool VerifyOtp(string normalizedEmail, string otp, string expectedHash);

        /// <summary>Sinh reset token opaque bằng CSPRNG (URL-safe).</summary>
        string GenerateResetToken();
    }
}
