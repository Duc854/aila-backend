using AILA.Application.Common.Interfaces;
using AILA.Application.Common.Dtos.AI;
using AILA.Application.Common.Interfaces.AI;
using AILA.Application.Common.Interfaces.Repositories;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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

    public async Task<QuotaCheckResultDto> CheckQuotaAsync(
        Guid accountId, 
        int estimatedTokens, 
        float warningThresholdPercent = 0.80f, 
        CancellationToken cancellationToken = default)
    {
        var activeUsage = await GetOrCreateActiveUsageAsync(accountId, cancellationToken);
        int currentUsed = activeUsage.AiTokenUsed;
        int dailyLimit = await ResolveDailyLimitAsync(accountId, cancellationToken);

        int remaining = Math.Max(0, dailyLimit - currentUsed);
        int percentUsed = (int)Math.Min(100, Math.Round((double)currentUsed / dailyLimit * 100));

        if (currentUsed + estimatedTokens > dailyLimit)
        {
            string limitWarning = $"⚠️ ĐÃ HẾT HẠN MỨC TOKEN: Bạn đã sử dụng {currentUsed:N0}/{dailyLimit:N0} Tokens trong kỳ hiện tại. Vui lòng nâng cấp gói đăng ký hoặc chờ kỳ mới.";
            return new QuotaCheckResultDto
            {
                IsAllowed = false,
                IsNearLimit = true,
                UsedAmount = currentUsed,
                DailyLimit = dailyLimit,
                RemainingTokens = remaining,
                PercentageUsed = percentUsed,
                WarningMessage = limitWarning
            };
        }

        int thresholdAmount = (int)(dailyLimit * warningThresholdPercent);
        bool isNearLimit = (currentUsed + estimatedTokens) >= thresholdAmount;

        string? warningMessage = isNearLimit
            ? $"⚠️ CẢNH BÁO HẠN MỨC: Bạn đã sử dụng {percentUsed}% hạn mức Token ({currentUsed:N0}/{dailyLimit:N0} Tokens, còn lại {remaining:N0} Tokens)."
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

    /// <summary>
    /// Lấy hoặc khởi tạo kỳ AccountResourceUsage (Quản lý theo 10 Lifecycle Rules):
    /// Case 1: Chưa có Usage -> Tạo mới với Used = 0, Period 30 ngày.
    /// Case 2: Usage còn hiệu lực (PeriodStart <= Now <= PeriodEnd) -> Dùng tiếp.
    /// Case 3: Usage hết hạn (> 30 ngày) -> Tạo mới kỳ 30 ngày với Used = 0.
    /// </summary>
    public async Task<AccountResourceUsage> GetOrCreateActiveUsageAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var usages = await _unitOfWork.Repository<AccountResourceUsage>()
            .FindAsync(u => u.AccountId == accountId && u.PeriodStart <= now && now <= u.PeriodEnd);

        var active = usages.OrderByDescending(u => u.PeriodStart).FirstOrDefault();

        if (active == null)
        {
            // Case 1 & Case 3: Tạo mới kỳ 30 ngày
            active = new AccountResourceUsage(accountId, now, now.AddDays(30));
            await _unitOfWork.Repository<AccountResourceUsage>().AddAsync(active);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return active;
    }

    /// <summary>
    /// Case 5 & Case 6: Kết thúc Usage hiện tại và tạo kỳ mới khi Upgrade Subscription hoặc Subscription hết hạn.
    /// </summary>
    public async Task ResetUsageForNewSubscriptionAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var activeUsages = await _unitOfWork.Repository<AccountResourceUsage>()
            .FindAsync(u => u.AccountId == accountId && u.PeriodEnd > now);

        foreach (var usage in activeUsages)
        {
            usage.ClosePeriod(now);
        }

        var newUsage = new AccountResourceUsage(accountId, now, now.AddDays(30));
        await _unitOfWork.Repository<AccountResourceUsage>().AddAsync(newUsage);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Tính toán Hạn mức Token ngày (Daily Token Limit) theo Cơ chế Ưu tiên 3 Cấp:
    /// Cấp 1 (Ưu tiên cao nhất): AccountResourceLimit (Custom override do Admin cài riêng cho Account)
    /// Cấp 2: Active Subscription Plan Snapshot (Gói đăng ký đang hoạt động Subscription.PlanSnapshot.AiTokenLimit)
    /// Cấp 3 (Mặc định): Default ResourceLimitPolicy (Chính sách mặc định theo loại tài khoản Learner/Expert)
    /// </summary>
    private async Task<int> ResolveDailyLimitAsync(Guid accountId, CancellationToken cancellationToken)
    {
        if (accountId == Guid.Empty) return 50000;

        // Cấp 1: Custom Override riêng cho Account do Admin thiết lập
        var accountOverride = await _unitOfWork.AccountResourceLimits.GetByAccountIdAsync(accountId, cancellationToken);
        if (accountOverride != null && accountOverride.AiTokenLimit.HasValue)
        {
            return accountOverride.AiTokenLimit.Value;
        }

        // Cấp 2: Active Subscription Plan Snapshot
        var activeSub = await _unitOfWork.Repository<Subscription>().FindAsync(s => 
            s.LearnerId == accountId && 
            s.Status == SubscriptionStatus.Active && 
            s.ExpiredAt >= DateTime.UtcNow);
        
        var currentActive = activeSub.FirstOrDefault();
        if (currentActive != null && currentActive.PlanSnapshot != null && currentActive.PlanSnapshot.AiTokenLimit > 0)
        {
            return currentActive.PlanSnapshot.AiTokenLimit;
        }

        // Cấp 3: Default ResourceLimitPolicy
        var defaultPolicy = await _unitOfWork.ResourceLimitPolicies.GetByAccountTypeAsync(ResourceAccountType.Learner, cancellationToken);
        if (defaultPolicy != null && defaultPolicy.AiTokenLimit > 0)
        {
            return defaultPolicy.AiTokenLimit;
        }

        // Fallback mặc định 50,000 tokens
        return 50000;
    }

    public async Task RecordTokenUsageAsync(
        Guid accountId, 
        Guid? attemptId, 
        string serviceType, 
        string modelId, 
        int promptTokens, 
        int completionTokens, 
        CancellationToken cancellationToken = default)
    {
        if (accountId == Guid.Empty) return;

        // 1. Log chi tiết vào AITokenLog
        var tokenLog = new AITokenLog(accountId, attemptId, serviceType, modelId, promptTokens, completionTokens);
        await _repository.AddTokenLogAsync(tokenLog, cancellationToken);

        // 2. Case 10: Người dùng sử dụng tài nguyên -> Cập nhật counter AiTokenUsed trong AccountResourceUsage
        var totalTokens = promptTokens + completionTokens;
        if (totalTokens > 0)
        {
            var activeUsage = await GetOrCreateActiveUsageAsync(accountId, cancellationToken);
            activeUsage.ConsumeAiToken(totalTokens);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
