using AILA.Application.Common.Interfaces;
using AILA.Application.Common.Dtos.AI;
using AILA.Application.Common.Interfaces.AI;
using AILA.Application.Common.Interfaces.Repositories;
using AILA.Domain.Entities;

namespace AILA.Infrastructure.Services.AI;

public class QuotaService : IQuotaService
{
    private readonly IAccountResourceRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public QuotaService(IAccountResourceRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> CheckAndConsumeQuotaAsync(Guid accountId, int estimatedTokens, CancellationToken cancellationToken = default)
    {
        var checkResult = await CheckQuotaAsync(accountId, estimatedTokens, 0.80f, cancellationToken);
        return checkResult.IsAllowed;
    }

    public async Task<QuotaCheckResultDto> CheckQuotaAsync(Guid accountId, int estimatedTokens, float warningThresholdPercent = 0.80f, CancellationToken cancellationToken = default)
    {
        var quota = await _repository.GetQuotaAsync(accountId, cancellationToken);
        if (quota == null)
        {
            // Default 50,000 daily limit for new users
            quota = new UserTokenQuota(accountId, 50000);
            await _repository.AddQuotaAsync(quota, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        if (!quota.CanConsume(estimatedTokens, out var warningMsg))
        {
            return new QuotaCheckResultDto
            {
                IsAllowed = false,
                IsNearLimit = true,
                UsedAmount = quota.UsedAmountToday,
                DailyLimit = quota.DailyLimit,
                RemainingTokens = Math.Max(0, quota.DailyLimit - quota.UsedAmountToday),
                PercentageUsed = (int)Math.Min(100, Math.Round((double)quota.UsedAmountToday / quota.DailyLimit * 100)),
                WarningMessage = warningMsg
            };
        }

        int currentUsed = quota.UsedAmountToday;
        int dailyLimit = quota.DailyLimit;
        int remaining = Math.Max(0, dailyLimit - currentUsed);
        int percentUsed = (int)Math.Min(100, Math.Round((double)currentUsed / dailyLimit * 100));

        int thresholdAmount = (int)(dailyLimit * warningThresholdPercent);
        bool isNearLimit = (currentUsed + estimatedTokens) >= thresholdAmount;

        string? warningMessage = isNearLimit
            ? $"⚠️ CẢNH BÁO HẠN MỨC: Bạn đã sử dụng {percentUsed}% hạn mức Token hôm nay ({currentUsed:N0}/{dailyLimit:N0} Tokens, còn lại {remaining:N0} Tokens)."
            : null;

        return new QuotaCheckResultDto
        {
            IsAllowed = true,
            IsNearLimit = isNearLimit,
            UsedAmount = currentUsed,
            DailyLimit = dailyLimit,
            RemainingTokens = remaining,
            PercentageUsed = percentUsed,
            WarningMessage = warningMessage
        };
    }

    public async Task RecordTokenUsageAsync(Guid accountId, Guid? attemptId, string serviceType, string modelId, int promptTokens, int completionTokens, CancellationToken cancellationToken = default)
    {
        if (accountId == Guid.Empty) return;

        var tokenLog = new AITokenLog(accountId, attemptId, serviceType, modelId, promptTokens, completionTokens);
        await _repository.AddTokenLogAsync(tokenLog, cancellationToken);

        var quota = await _repository.GetQuotaAsync(accountId, cancellationToken);
        var totalTokens = promptTokens + completionTokens;

        if (quota == null)
        {
            quota = new UserTokenQuota(accountId, 50000);
            await _repository.AddQuotaAsync(quota, cancellationToken);
        }

        quota.RecordUsage(totalTokens);
    }
}
