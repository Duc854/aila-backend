using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;
using Shared.Models;

namespace AILA.Infrastructure.Services.Email
{
    /// <summary>Gửi email qua SMTP bằng MailKit.</summary>
    public sealed class SmtpEmailTransport : IEmailTransport
    {
        private readonly SmtpSettings _settings;

        public SmtpEmailTransport(IOptions<SmtpSettings> settings)
        {
            _settings = settings.Value;
        }

        public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            var mail = new MimeMessage();
            mail.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
            mail.To.Add(new MailboxAddress(message.ToName, message.ToEmail));
            mail.Subject = message.Subject;

            var builder = new BodyBuilder
            {
                HtmlBody = message.HtmlBody,
                TextBody = message.TextBody
            };
            mail.Body = builder.ToMessageBody();

            using var client = new SmtpClient
            {
                Timeout = _settings.TimeoutSeconds * 1000
            };

            var secureOption = _settings.UseStartTls
                ? SecureSocketOptions.StartTls
                : SecureSocketOptions.SslOnConnect;

            await client.ConnectAsync(_settings.Host, _settings.Port, secureOption, cancellationToken);

            if (!string.IsNullOrWhiteSpace(_settings.UserName))
                await client.AuthenticateAsync(_settings.UserName, _settings.Password, cancellationToken);

            await client.SendAsync(mail, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
        }

        // Chỉ dùng cho log/diagnostic — không bao giờ ghi nội dung email ra log.
        public override string ToString() => $"SMTP {_settings.Host}:{_settings.Port}";
    }
}
