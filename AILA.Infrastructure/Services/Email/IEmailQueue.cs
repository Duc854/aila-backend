namespace AILA.Infrastructure.Services.Email
{
    /// <summary>
    /// Hàng đợi trong tiến trình giữa "web request" và "worker gửi mail".
    /// Nhờ nó mà endpoint request-OTP trả về ngay, không chờ SMTP bắt tay (NFR Performance).
    /// </summary>
    public interface IEmailQueue
    {
        /// <summary>Đưa email vào hàng đợi. Không bao giờ block.</summary>
        ValueTask EnqueueAsync(EmailMessage message, CancellationToken cancellationToken = default);

        /// <summary>Luồng email chờ gửi, dành cho background worker đọc.</summary>
        IAsyncEnumerable<EmailMessage> DequeueAllAsync(CancellationToken cancellationToken);
    }
}
