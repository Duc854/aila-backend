using AILA.Application.Common.Interfaces;
using AILA.Application.Common.Interfaces.Repositories;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using AILA.Infrastructure.Services.AI;
using Moq;
using System.Linq.Expressions;

namespace AILA.Application.Tests.UnitTests;

/// <summary>
/// Sheet: UT08_CheckQuota — <see cref="QuotaService.CheckQuotaAsync"/>
/// Module: AIPractice · CC = 9 · 11 test case
///
/// Nhánh: B1 = chưa có kỳ AccountResourceUsage đang hiệu lực (tạo mới + ghi DB)
///        B2 = (used + est) &gt; dailyLimit ⇒ TỪ CHỐI, đặt CỨNG IsNearLimit = true
///        B3/B4 = Math.Max(0, …) / Math.Min(100, …) kẹp giá trị khi used vượt limit
///        B5 = (used + est) &gt;= thresholdAmount · B6 = toán tử ?: sinh WarningMessage
///
/// LƯU Ý THAY ĐỔI MÔ HÌNH DỮ LIỆU: entity UserTokenQuota (hạn mức reset theo NGÀY) đã bị gỡ.
/// Hạn mức nay tính theo KỲ 30 ngày qua <see cref="AccountResourceUsage"/>, còn trần token
/// được ResolveDailyLimitAsync quyết định theo cơ chế ưu tiên 3 cấp:
///   Cấp 1 AccountResourceLimit (admin override) → Cấp 2 Subscription.PlanSnapshot
///   → Cấp 3 ResourceLimitPolicy → fallback 50.000.
/// UTCID09–UTCID11 phủ riêng ba cấp này.
///
/// Bẫy dễ sai: PercentageUsed tính trên currentUsed, KHÔNG cộng estimatedTokens —
/// trong khi IsNearLimit lại so sánh (currentUsed + estimatedTokens). UTCID05 chốt điều đó.
/// </summary>
public class UT08_QuotaService_CheckQuotaTests
{
    private const int DefaultDailyLimit = 50_000;

    private readonly Mock<IAccountResourceRepository> _repo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IGenericRepository<AccountResourceUsage>> _usages = new();
    private readonly Mock<IGenericRepository<Subscription>> _subscriptions = new();
    private readonly Mock<IAccountResourceLimitRepository> _limits = new();
    private readonly Mock<IResourceLimitPolicyRepository> _policies = new();
    private readonly Guid _accountId = Guid.NewGuid();

    public UT08_QuotaService_CheckQuotaTests()
    {
        _uow.Setup(x => x.Repository<AccountResourceUsage>()).Returns(_usages.Object);
        _uow.Setup(x => x.Repository<Subscription>()).Returns(_subscriptions.Object);
        _uow.SetupGet(x => x.AccountResourceLimits).Returns(_limits.Object);
        _uow.SetupGet(x => x.ResourceLimitPolicies).Returns(_policies.Object);

        // Mặc định: cả ba cấp override đều trống ⇒ rơi về fallback 50.000
        _limits.Setup(x => x.GetByAccountIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync((AccountResourceLimit?)null);
        _subscriptions.Setup(x => x.FindAsync(It.IsAny<Expression<Func<Subscription, bool>>>()))
                      .ReturnsAsync(Array.Empty<Subscription>());
        _policies.Setup(x => x.GetByAccountTypeAsync(It.IsAny<ResourceAccountType>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((ResourceLimitPolicy?)null);
    }

    private QuotaService CreateSut() => new(_repo.Object, _uow.Object);

    /// <summary>Dựng sẵn một kỳ đang hiệu lực với số token đã tiêu thụ cho trước.</summary>
    private AccountResourceUsage SetupUsage(int usedTokens)
    {
        var now = DateTime.UtcNow;
        var usage = new AccountResourceUsage(_accountId, now.AddDays(-1), now.AddDays(29));
        if (usedTokens > 0)
            usage.ConsumeAiToken(usedTokens);

        _usages.Setup(x => x.FindAsync(It.IsAny<Expression<Func<AccountResourceUsage, bool>>>()))
               .ReturnsAsync(new[] { usage });
        return usage;
    }

    /// <summary>Không có kỳ nào đang hiệu lực ⇒ ép service đi vào nhánh tạo kỳ mới.</summary>
    private void SetupNoActiveUsage() =>
        _usages.Setup(x => x.FindAsync(It.IsAny<Expression<Func<AccountResourceUsage, bool>>>()))
               .ReturnsAsync(Array.Empty<AccountResourceUsage>());

    private void AssertNoUsageCreated()
    {
        _usages.Verify(x => x.AddAsync(It.IsAny<AccountResourceUsage>()), Times.Never);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// UTCID01 · B1=T, B2=F, B5=F · Type N — người dùng mới chưa có kỳ nào.
    /// Tác dụng phụ: tạo kỳ 30 ngày với Used = 0 và ghi DB ngay.
    /// </summary>
    [Fact]
    public async Task UTCID01_NewUserWithoutUsagePeriod_CreatesPeriodAndAllows()
    {
        SetupNoActiveUsage();

        var result = await CreateSut().CheckQuotaAsync(_accountId, 1_000);

        Assert.True(result.IsAllowed);
        Assert.False(result.IsNearLimit);
        Assert.Equal(0, result.UsedAmount);
        Assert.Equal(DefaultDailyLimit, result.DailyLimit);
        Assert.Equal(DefaultDailyLimit, result.RemainingTokens);
        Assert.Equal(0, result.PercentageUsed);
        Assert.Null(result.WarningMessage);

        _usages.Verify(x => x.AddAsync(It.Is<AccountResourceUsage>(u =>
            u.AccountId == _accountId && u.AiTokenUsed == 0)), Times.Once);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>UTCID02 · B1=F, B2=F, B5=F · Type N — kỳ đã tồn tại, không ghi thêm DB.</summary>
    [Fact]
    public async Task UTCID02_ExistingPeriodWithinLimit_DoesNotCreatePeriod()
    {
        SetupUsage(usedTokens: 1_000);

        var result = await CreateSut().CheckQuotaAsync(_accountId, 1_000);

        Assert.True(result.IsAllowed);
        Assert.False(result.IsNearLimit);
        Assert.Equal(1_000, result.UsedAmount);
        Assert.Equal(49_000, result.RemainingTokens);
        Assert.Equal(2, result.PercentageUsed);
        Assert.Null(result.WarningMessage);
        AssertNoUsageCreated();
    }

    /// <summary>
    /// UTCID03 · B2=F · Type B — tổng BẰNG ĐÚNG dailyLimit ⇒ vẫn cho phép (toán tử &gt;, không phải &gt;=).
    /// Đây là biên đắt giá nhất của method.
    ///
    /// Cả RemainingTokens (1.000) lẫn PercentageUsed (98 %) đều tính trên currentUsed = 49.000,
    /// KHÔNG trừ/cộng estimatedTokens — tức là số liệu báo cho người dùng phản ánh trạng thái
    /// TRƯỚC lượt gọi này, dù lượt gọi đó sẽ dùng hết sạch phần còn lại.
    /// </summary>
    [Fact]
    public async Task UTCID03_TotalExactlyEqualsDailyLimit_ReturnsAllowed()
    {
        SetupUsage(usedTokens: 49_000);

        var result = await CreateSut().CheckQuotaAsync(_accountId, 1_000);

        Assert.True(result.IsAllowed);
        Assert.Equal(1_000, result.RemainingTokens);
        Assert.Equal(98, result.PercentageUsed);
    }

    /// <summary>UTCID04 · B2=T · Type A — vượt hạn mức: nhánh này đặt CỨNG IsNearLimit = true.</summary>
    [Fact]
    public async Task UTCID04_QuotaExhausted_ReturnsNotAllowedWithNearLimitTrue()
    {
        SetupUsage(usedTokens: DefaultDailyLimit);

        var result = await CreateSut().CheckQuotaAsync(_accountId, 1);

        Assert.False(result.IsAllowed);
        Assert.True(result.IsNearLimit);
        Assert.Equal(0, result.RemainingTokens);
        Assert.Equal(100, result.PercentageUsed);
        Assert.NotNull(result.WarningMessage);
        // Thông điệp dùng format "N0" nên phụ thuộc CurrentCulture của tiến trình
        // (vi-VN → "50.000", en-US → "50,000"). Assert theo đúng culture đang chạy
        // để test ổn định trên mọi máy CI.
        Assert.Contains(DefaultDailyLimit.ToString("N0"), result.WarningMessage);
        AssertNoUsageCreated();
    }

    /// <summary>
    /// UTCID05 · B5=T, B6 · Type B — tổng BẰNG ĐÚNG ngưỡng 40.000 ⇒ cảnh báo (toán tử &gt;=).
    /// Đồng thời chốt bẫy: PercentageUsed = 78 % tính trên 39.000 đã dùng, KHÔNG phải trên
    /// 40.000 (đã cộng estimatedTokens) — hai vế dùng công thức khác nhau.
    /// </summary>
    [Fact]
    public async Task UTCID05_ExactlyAtWarningThreshold_WarnsUsingUsedOnlyPercentage()
    {
        SetupUsage(usedTokens: 39_000);

        var result = await CreateSut().CheckQuotaAsync(_accountId, 1_000);

        Assert.True(result.IsAllowed);
        Assert.True(result.IsNearLimit);
        Assert.NotNull(result.WarningMessage);
        Assert.Equal(78, result.PercentageUsed);
        Assert.Contains("78%", result.WarningMessage);
    }

    /// <summary>UTCID06 · B5=F · Type B — tổng 39.999 &lt; ngưỡng 40.000 ⇒ chưa cảnh báo.</summary>
    [Fact]
    public async Task UTCID06_JustBelowWarningThreshold_DoesNotWarn()
    {
        SetupUsage(usedTokens: 39_000);

        var result = await CreateSut().CheckQuotaAsync(_accountId, 999);

        Assert.True(result.IsAllowed);
        Assert.False(result.IsNearLimit);
        Assert.Null(result.WarningMessage);
        Assert.Equal(78, result.PercentageUsed);
    }

    /// <summary>UTCID07 · B5=T · Type B — ngưỡng cảnh báo 0 % ⇒ luôn cảnh báo (0 &gt;= 0).</summary>
    [Fact]
    public async Task UTCID07_ZeroWarningThreshold_AlwaysWarns()
    {
        SetupUsage(usedTokens: 0);

        var result = await CreateSut().CheckQuotaAsync(_accountId, 0, warningThresholdPercent: 0.0f);

        Assert.True(result.IsAllowed);
        Assert.True(result.IsNearLimit);
        Assert.NotNull(result.WarningMessage);
    }

    /// <summary>UTCID08 · B5=F · Type B — ngưỡng cảnh báo 100 % ⇒ chỉ cảnh báo khi chạm trần.</summary>
    [Fact]
    public async Task UTCID08_FullWarningThreshold_DoesNotWarnBelowLimit()
    {
        SetupUsage(usedTokens: 40_000);

        var result = await CreateSut().CheckQuotaAsync(_accountId, 1_000, warningThresholdPercent: 1.0f);

        Assert.True(result.IsAllowed);
        Assert.False(result.IsNearLimit);
        Assert.Null(result.WarningMessage);
        Assert.Equal(80, result.PercentageUsed);
    }

    /// <summary>
    /// UTCID09 · Cấp 1 · Type N — admin đặt override riêng cho tài khoản (10.000 token).
    /// Override phải THẮNG mọi cấp dưới.
    /// </summary>
    [Fact]
    public async Task UTCID09_AccountOverrideLimit_TakesHighestPriority()
    {
        SetupUsage(usedTokens: 5_000);
        _limits.Setup(x => x.GetByAccountIdAsync(_accountId, It.IsAny<CancellationToken>()))
               .ReturnsAsync(new AccountResourceLimit(_accountId, aiTokenLimit: 10_000));

        var result = await CreateSut().CheckQuotaAsync(_accountId, 1_000);

        Assert.Equal(10_000, result.DailyLimit);
        Assert.Equal(5_000, result.RemainingTokens);
        Assert.Equal(50, result.PercentageUsed);
    }

    /// <summary>
    /// UTCID10 · Cấp 3 · Type N — không có override, không có subscription
    /// ⇒ lấy trần từ ResourceLimitPolicy mặc định của Learner.
    /// </summary>
    [Fact]
    public async Task UTCID10_DefaultPolicyLimit_UsedWhenNoOverrideOrSubscription()
    {
        SetupUsage(usedTokens: 5_000);
        _policies.Setup(x => x.GetByAccountTypeAsync(ResourceAccountType.Learner, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new ResourceLimitPolicy(ResourceAccountType.Learner, 20_000, 10, 5));

        var result = await CreateSut().CheckQuotaAsync(_accountId, 1_000);

        Assert.Equal(20_000, result.DailyLimit);
        Assert.Equal(15_000, result.RemainingTokens);
        Assert.Equal(25, result.PercentageUsed);
    }

    /// <summary>
    /// UTCID11 · B2=T, B3/B4 · Type A — admin HẠ trần token sau khi người dùng đã tiêu thụ vượt.
    /// Là case DUY NHẤT chạm được nhánh kẹp Math.Max(0, …) và Math.Min(100, …):
    /// không có clamp thì RemainingTokens sẽ âm và PercentageUsed vượt 100.
    /// </summary>
    [Fact]
    public async Task UTCID11_UsedExceedsLoweredLimit_ClampsRemainingAndPercentage()
    {
        SetupUsage(usedTokens: 60_000);
        _limits.Setup(x => x.GetByAccountIdAsync(_accountId, It.IsAny<CancellationToken>()))
               .ReturnsAsync(new AccountResourceLimit(_accountId, aiTokenLimit: DefaultDailyLimit));

        var result = await CreateSut().CheckQuotaAsync(_accountId, 0);

        Assert.False(result.IsAllowed);
        Assert.Equal(0, result.RemainingTokens);
        Assert.Equal(100, result.PercentageUsed);
    }
}
