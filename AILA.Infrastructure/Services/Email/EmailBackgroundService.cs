using AILA.Application.Common.Helpers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Models;

namespace AILA.Infrastructure.Services.Email
{
    /// <summary>
    /// Worker đọc hàng đợi và gửi email, có retry (NFR Reliability).
    /// Chạy ngoài vòng đời request nên sự cố của Email Service không ảnh hưởng tới
    /// status hay thời gian phản hồi của endpoint reset (EDGE-11).
    /// </summary>
    public sealed class EmailBackgroundService : BackgroundService
    {
        private readonly IEmailQueue _queue;
        private readonly IEmailTransport _transport;
        private readonly SmtpSettings _settings;
        private readonly ILogger<EmailBackgroundService> _logger;

        public EmailBackgroundService(
            IEmailQueue queue,
            IEmailTransport transport,
            IOptions<SmtpSettings> settings,
            ILogger<EmailBackgroundService> logger)
        {
            _queue = queue;
            _transport = transport;
            _settings = settings.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await foreach (var message in _queue.DequeueAllAsync(stoppingToken))
            {
                await SendWithRetryAsync(message, stoppingToken);
            }
        }

        private async Task SendWithRetryAsync(EmailMessage message, CancellationToken stoppingToken)
        {
            var maxAttempts = Math.Max(1, _settings.MaxRetryAttempts);
            var maskedTo = EmailHelper.Mask(message.ToEmail);

            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    await _transport.SendAsync(message, stoppingToken);

                    _logger.LogInformation(
                        "Gửi email thành công. to={To}, attempt={Attempt}", maskedTo, attempt);
                    return;
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // Ứng dụng đang tắt — dừng hẳn, không coi là lỗi gửi mail.
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Gửi email thất bại. to={To}, attempt={Attempt}/{MaxAttempts}",
                        maskedTo, attempt, maxAttempts);

                    if (attempt == maxAttempts)
                    {
                        _logger.LogError(ex,
                            "Bỏ cuộc sau {MaxAttempts} lần gửi email. to={To}", maxAttempts, maskedTo);
                        return;
                    }

                    try
                    {
                        await Task.Delay(
                            TimeSpan.FromSeconds(_settings.RetryDelaySeconds * attempt),
                            stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }
                }
            }
        }
    }
}
