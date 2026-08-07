using AILA.Application.Common.Interfaces;
using AILA.Application.Common.Interfaces.Repositories;
using AILA.Application.Tests.UnitTests.TestHelpers;
using AILA.Domain.Entities;
using AILA.Infrastructure.Services.AI;
using Moq;

namespace AILA.Application.Tests.UnitTests;

/// <summary>
/// Sheet: UT11_CheckQuota — <see cref="QuotaService.CheckQuotaAsync"/>
/// Module: AIPractice · CC = 9 · 10 test case
///
/// Nhánh: B1 = quota == null (tạo mặc định 50.000, CÓ GHI DB)
///        B2 = !quota.CanConsume(...) · B3/B4 = Math.Max/Math.Min ở nhánh từ chối
///        B5/B6 = Math.Max/Math.Min ở nhánh cho phép
///        B7 = (used + est) &gt;= threshold · B8 = toán tử ?: sinh WarningMessage
/// </summary>
public class UT11_QuotaService_CheckQuotaTests
{
    private const int DefaultDailyLimit = 50_000;

    private readonly Mock<IAccountResourceRepository> _repo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Guid _accountId = Guid.NewGuid();

    private QuotaService CreateSut() => new(_repo.Object, _uow.Object);

    private UserTokenQuota SetupQuota(int usedToday, int dailyLimit = DefaultDailyLimit)
    {
        var quota = new UserTokenQuota(_accountId, dailyLimit);
        PrivateSetter.Set(quota, nameof(UserTokenQuota.UsedAmountToday), usedToday);
        PrivateSetter.Set(quota, nameof(UserTokenQuota.LastUsageDate), DateTime.UtcNow.Date);

        _repo.Setup(x => x.GetQuotaAsync(_accountId, It.IsAny<CancellationToken>())).ReturnsAsync(quota);
        return quota;
    }

    private void AssertNoQuotaCreated()
    {
        _repo.Verify(x => x.AddQuotaAsync(It.IsAny<UserTokenQuota>(), It.IsAny<CancellationToken>()), Times.Never);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// UTCID01 · B1=T, B2=F, B7=F · Type N — người dùng mới.
    /// Tác dụng phụ: tạo quota mặc định 50.000 và ghi DB ngay.
    /// </summary>
    [Fact]
    public async Task UTCID01_NewUserWithoutQuota_CreatesDefaultQuotaAndAllows()
    {
        _repo.Setup(x => x.GetQuotaAsync(_accountId, It.IsAny<CancellationToken>()))
             .ReturnsAsync((UserTokenQuota?)null);

        var result = await CreateSut().CheckQuotaAsync(_accountId, 1_000);

        Assert.True(result.IsAllowed);
        Assert.False(result.IsNearLimit);
        Assert.Equal(0, result.UsedAmount);
        Assert.Equal(DefaultDailyLimit, result.DailyLimit);
        Assert.Equal(DefaultDailyLimit, result.RemainingTokens);
        Assert.Equal(0, result.PercentageUsed);
        Assert.Null(result.WarningMessage);

        _repo.Verify(x => x.AddQuotaAsync(
            It.Is<UserTokenQuota>(q => q.DailyLimit == DefaultDailyLimit), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>UTCID02 · B1=F, B2=F, B7=F · Type N — quota đã tồn tại, không ghi thêm DB.</summary>
    [Fact]
    public async Task UTCID02_ExistingQuotaWithinLimit_DoesNotCreateQuota()
    {
        SetupQuota(usedToday: 1_000);

        var result = await CreateSut().CheckQuotaAsync(_accountId, 1_000);

        Assert.True(result.IsAllowed);
        Assert.False(result.IsNearLimit);
        Assert.Equal(1_000, result.UsedAmount);
        Assert.Equal(49_000, result.RemainingTokens);
        Assert.Equal(2, result.PercentageUsed);
        Assert.Null(result.WarningMessage);
        AssertNoQuotaCreated();
    }

    /// <summary>UTCID03 · B2=T · Type A — vượt hạn mức: nhánh này đặt CỨNG IsNearLimit = true.</summary>
    [Fact]
    public async Task UTCID03_QuotaExhausted_ReturnsNotAllowedWithNearLimitTrue()
    {
        SetupQuota(usedToday: DefaultDailyLimit);

        var result = await CreateSut().CheckQuotaAsync(_accountId, 1);

        Assert.False(result.IsAllowed);
        Assert.True(result.IsNearLimit);
        Assert.Equal(0, result.RemainingTokens);
        Assert.Equal(100, result.PercentageUsed);
        Assert.NotNull(result.WarningMessage);
        AssertNoQuotaCreated();
    }

    /// <summary>UTCID04 · B7=F · Type B — tổng 39.999 &lt; ngưỡng 40.000 ⇒ chưa cảnh báo.</summary>
    [Fact]
    public async Task UTCID04_JustBelowWarningThreshold_DoesNotWarn()
    {
        SetupQuota(usedToday: 39_000);

        var result = await CreateSut().CheckQuotaAsync(_accountId, 999);

        Assert.True(result.IsAllowed);
        Assert.False(result.IsNearLimit);
        Assert.Null(result.WarningMessage);
        Assert.Equal(78, result.PercentageUsed);
    }

    /// <summary>UTCID05 · B7=T, B8 · Type B — tổng BẰNG ĐÚNG ngưỡng 40.000 ⇒ cảnh báo (toán tử &gt;=).</summary>
    [Fact]
    public async Task UTCID05_ExactlyAtWarningThreshold_Warns()
    {
        SetupQuota(usedToday: 39_000);

        var result = await CreateSut().CheckQuotaAsync(_accountId, 1_000);

        Assert.True(result.IsAllowed);
        Assert.True(result.IsNearLimit);
        Assert.NotNull(result.WarningMessage);
        Assert.Contains("78%", result.WarningMessage);
    }

    /// <summary>UTCID06 · B7=T · Type N — đã dùng 90 % hạn mức.</summary>
    [Fact]
    public async Task UTCID06_NearLimit_WarnsWithCorrectPercentage()
    {
        SetupQuota(usedToday: 45_000);

        var result = await CreateSut().CheckQuotaAsync(_accountId, 1_000);

        Assert.True(result.IsAllowed);
        Assert.True(result.IsNearLimit);
        Assert.Equal(90, result.PercentageUsed);
        Assert.Contains("90%", result.WarningMessage!);
    }

    /// <summary>UTCID07 · B7=T · Type B — ngưỡng cảnh báo 0 % ⇒ luôn cảnh báo (0 &gt;= 0).</summary>
    [Fact]
    public async Task UTCID07_ZeroWarningThreshold_AlwaysWarns()
    {
        SetupQuota(usedToday: 0);

        var result = await CreateSut().CheckQuotaAsync(_accountId, 0, warningThresholdPercent: 0.0f);

        Assert.True(result.IsAllowed);
        Assert.True(result.IsNearLimit);
        Assert.NotNull(result.WarningMessage);
    }

    /// <summary>UTCID08 · B7=F · Type B — ngưỡng cảnh báo 100 % ⇒ chỉ cảnh báo khi chạm trần.</summary>
    [Fact]
    public async Task UTCID08_FullWarningThreshold_DoesNotWarnBelowLimit()
    {
        SetupQuota(usedToday: 40_000);

        var result = await CreateSut().CheckQuotaAsync(_accountId, 1_000, warningThresholdPercent: 1.0f);

        Assert.True(result.IsAllowed);
        Assert.False(result.IsNearLimit);
        Assert.Null(result.WarningMessage);
        Assert.Equal(80, result.PercentageUsed);
    }

    /// <summary>
    /// UTCID09 · B2=T, B3/B4 · Type A — admin hạ DailyLimit sau khi người dùng đã tiêu thụ vượt.
    /// Là case DUY NHẤT chạm được nhánh kẹp Math.Max(0, …) và Math.Min(100, …).
    /// </summary>
    [Fact]
    public async Task UTCID09_UsedExceedsLoweredDailyLimit_ClampsRemainingAndPercentage()
    {
        SetupQuota(usedToday: 60_000, dailyLimit: DefaultDailyLimit);

        var result = await CreateSut().CheckQuotaAsync(_accountId, 0);

        Assert.False(result.IsAllowed);
        Assert.Equal(0, result.RemainingTokens);
        Assert.Equal(100, result.PercentageUsed);
    }

    /// <summary>
    /// UTCID10 · B6 · Type B — 1/8 = 12,5 %.
    /// DEFECT DFID002: Math.Round mặc định dùng banker's rounding ⇒ 12,5 → 12,
    /// tức BÁO THIẾU mức đã dùng cho người dùng. SubmitQuizCommandHandler cùng hệ thống
    /// đã dùng MidpointRounding.AwayFromZero ⇒ hai chỗ không nhất quán.
    /// Expected ghi theo hành vi ĐÚNG (13) ⇒ test này DỰ KIẾN FAIL.
    /// </summary>
    [Fact]
    public async Task UTCID10_PercentageAtMidpoint_ShouldRoundAwayFromZero()
    {
        SetupQuota(usedToday: 1, dailyLimit: 8);

        var result = await CreateSut().CheckQuotaAsync(_accountId, 1);

        Assert.True(result.IsAllowed);
        Assert.Equal(13, result.PercentageUsed);
    }
}
