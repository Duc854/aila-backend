using AILA.Application.Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AILA.Infrastructure.Services.Quizzes
{
    /// <summary>
    /// Worker quét và đóng các lượt làm bài đã hết giờ (AF-01, DEF-QA-01).
    /// Nếu máy khách không bao giờ gửi bài nộp (đóng tab, mất mạng, cố tình giữ lượt),
    /// lượt làm bài sẽ treo ở InProgress vĩnh viễn; worker này đảm bảo đồng hồ đếm giờ
    /// là của SERVER, không phụ thuộc việc máy khách có gọi API nộp bài hay không.
    /// </summary>
    public sealed class QuizAttemptExpiryService : BackgroundService
    {
        private static readonly TimeSpan SweepInterval = TimeSpan.FromMinutes(5);

        // Giới hạn mỗi lượt quét để không giữ transaction quá lâu; phần còn lại
        // sẽ được xử lý ở các lượt quét kế tiếp.
        private const int BatchSize = 200;

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<QuizAttemptExpiryService> _logger;

        public QuizAttemptExpiryService(
            IServiceScopeFactory scopeFactory,
            ILogger<QuizAttemptExpiryService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(SweepInterval);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await SweepAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    // Sự cố của một lượt quét không được làm chết worker — thử lại ở lượt sau.
                    _logger.LogError(ex, "Lượt quét đóng bài kiểm tra hết giờ thất bại.");
                }

                try
                {
                    if (!await timer.WaitForNextTickAsync(stoppingToken))
                        return;
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }

        private async Task SweepAsync(CancellationToken cancellationToken)
        {
            // BackgroundService là singleton còn IUnitOfWork là scoped nên phải tự mở scope.
            using var scope = _scopeFactory.CreateScope();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var now = DateTime.UtcNow;
            var overdueAttempts = await uow.Quizzes.GetOverdueInProgressAttemptsAsync(
                now, BatchSize, cancellationToken);

            if (overdueAttempts.Count == 0)
                return;

            foreach (var attempt in overdueAttempts)
            {
                attempt.Expire();
            }

            await uow.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Đã đóng {Count} lượt làm bài kiểm tra hết giờ (mốc quét {Now:O}).",
                overdueAttempts.Count, now);
        }
    }
}
