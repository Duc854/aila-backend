namespace AILA.Application.Common.Interfaces
{
    /// <summary>
    /// Cổng ra Email Service. Cài đặt phải trả về ngay (enqueue) để không block response
    /// của endpoint request-OTP, và lỗi gửi email không được làm lộ việc email có tồn tại
    /// hay không (EDGE-11).
    /// </summary>
    public interface IEmailSender
    {
        /// <summary>Gửi email chứa OTP reset password (OTP ở dạng plaintext trong email).</summary>
        Task SendPasswordResetOtpAsync(
            string toEmail,
            string fullName,
            string otp,
            int expiresInMinutes,
            CancellationToken cancellationToken = default);
    }
}
