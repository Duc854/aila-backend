namespace AILA.Application.Common.Interfaces
{
    /// <summary>
    /// Cổng ra Email Service. Cài đặt gửi đồng bộ trong vòng đời request, nên lời gọi chỉ
    /// hoàn tất khi email đã được gửi (hoặc ném lỗi). Phía gọi có trách nhiệm nuốt lỗi gửi
    /// để không làm lộ việc email có tồn tại hay không (EDGE-11).
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
