namespace AILA.Infrastructure.Services.Email
{
    /// <summary>
    /// Lớp thực sự đẩy email ra ngoài. Tách khỏi <see cref="IEmailQueue"/> để đổi nhà cung cấp
    /// (SMTP, SendGrid, SES…) mà không đụng tới phần hàng đợi hay tầng Application.
    /// </summary>
    public interface IEmailTransport
    {
        Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
    }
}
