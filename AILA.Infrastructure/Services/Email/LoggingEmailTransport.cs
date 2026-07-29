using AILA.Application.Common.Helpers;
using Microsoft.Extensions.Logging;

namespace AILA.Infrastructure.Services.Email
{
    /// <summary>
    /// Transport dự phòng khi chưa cấu hình SMTP: chỉ ghi log là "đã gửi", để môi trường dev
    /// chạy được luồng reset mà không cần mail server.
    ///
    /// <para>
    /// Nội dung email (kèm OTP) <b>không</b> được ghi ra log ở mức Information — OTP là bí mật.
    /// Muốn xem OTP khi dev thì bật log level Debug cho namespace này.
    /// </para>
    /// </summary>
    public sealed class LoggingEmailTransport : IEmailTransport
    {
        private readonly ILogger<LoggingEmailTransport> _logger;

        public LoggingEmailTransport(ILogger<LoggingEmailTransport> logger)
        {
            _logger = logger;
        }

        public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "SMTP chưa được cấu hình — bỏ qua việc gửi email thật. to={To}, subject={Subject}",
                EmailHelper.Mask(message.ToEmail), message.Subject);

            _logger.LogDebug("Nội dung email (chỉ dùng khi dev): {Body}", message.TextBody);

            return Task.CompletedTask;
        }
    }
}
