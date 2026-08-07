using AILA.Application.Tests.UnitTests.TestHelpers;
using AILA.Domain.Entities;

namespace AILA.Application.Tests.UnitTests;

/// <summary>
/// Sheet: UT10_CanConsume — <see cref="UserTokenQuota.CanConsume"/>
/// Module: AIPractice · CC = 3 · 7 test case
///
/// Nhánh: B1 = LastUsageDate.Date &lt; today (reset theo ngày, CÓ TÁC DỤNG PHỤ)
///        B2 = UsedAmountToday + estimatedTokens &gt; DailyLimit
///
/// B2 dùng toán tử &gt; (không phải &gt;=) ⇒ tổng BẰNG ĐÚNG DailyLimit vẫn được phép.
/// Đây là biên đắt giá nhất của method.
/// </summary>
public class UT10_UserTokenQuota_CanConsumeTests
{
    private const int DailyLimit = 50_000;

    private static UserTokenQuota BuildQuota(int usedToday, DateTime? lastUsageDate = null)
    {
        var quota = new UserTokenQuota(Guid.NewGuid(), DailyLimit);
        PrivateSetter.Set(quota, nameof(UserTokenQuota.UsedAmountToday), usedToday);
        PrivateSetter.Set(quota, nameof(UserTokenQuota.LastUsageDate), lastUsageDate ?? DateTime.UtcNow.Date);
        return quota;
    }

    /// <summary>UTCID01 · B1=F, B2=F · Type N — còn dư hạn mức.</summary>
    [Fact]
    public void UTCID01_WithinQuota_ReturnsTrueWithoutWarning()
    {
        var quota = BuildQuota(usedToday: 0);

        var result = quota.CanConsume(1_000, out var warning);

        Assert.True(result);
        Assert.Null(warning);
        Assert.Equal(0, quota.UsedAmountToday);
    }

    /// <summary>UTCID02 · B2=F · Type B — tổng BẰNG ĐÚNG DailyLimit ⇒ vẫn cho phép.</summary>
    [Fact]
    public void UTCID02_TotalExactlyEqualsDailyLimit_ReturnsTrue()
    {
        var quota = BuildQuota(usedToday: 49_000);

        var result = quota.CanConsume(1_000, out var warning);

        Assert.True(result);
        Assert.Null(warning);
    }

    /// <summary>UTCID03 · B2=T · Type B — tổng vượt DailyLimit đúng 1 token ⇒ từ chối.</summary>
    [Fact]
    public void UTCID03_TotalExceedsDailyLimitByOne_ReturnsFalseWithWarning()
    {
        var quota = BuildQuota(usedToday: 49_000);

        var result = quota.CanConsume(1_001, out var warning);

        Assert.False(result);
        Assert.NotNull(warning);
        // Thông điệp dùng format "N0" nên phụ thuộc CurrentCulture của tiến trình
        // (vi-VN → "49.000", en-US → "49,000"). Assert theo đúng culture đang chạy
        // để test ổn định trên mọi máy CI.
        Assert.Contains(49_000.ToString("N0"), warning);
        Assert.Contains(DailyLimit.ToString("N0"), warning);
    }

    /// <summary>UTCID04 · B2=T · Type B — đã dùng hết hạn mức, xin thêm 1 token.</summary>
    [Fact]
    public void UTCID04_QuotaAlreadyExhausted_ReturnsFalse()
    {
        var quota = BuildQuota(usedToday: DailyLimit);

        var result = quota.CanConsume(1, out var warning);

        Assert.False(result);
        Assert.NotNull(warning);
    }

    /// <summary>UTCID05 · B2=F · Type B — estimatedTokens = 0 khi đã dùng hết ⇒ vẫn cho phép.</summary>
    [Fact]
    public void UTCID05_ZeroEstimatedTokensAtExactLimit_ReturnsTrue()
    {
        var quota = BuildQuota(usedToday: DailyLimit);

        var result = quota.CanConsume(0, out var warning);

        Assert.True(result);
        Assert.Null(warning);
    }

    /// <summary>
    /// UTCID06 · B1=T · Type N — quota của ngày hôm qua tự reset về 0.
    /// Phải assert TRẠNG THÁI sau khi gọi, không chỉ giá trị trả về (B1 có tác dụng phụ).
    /// </summary>
    [Fact]
    public void UTCID06_QuotaFromYesterday_ResetsUsedAmountAndAllows()
    {
        var quota = BuildQuota(usedToday: DailyLimit, lastUsageDate: DateTime.UtcNow.Date.AddDays(-1));

        var result = quota.CanConsume(1_000, out var warning);

        Assert.True(result);
        Assert.Null(warning);
        Assert.Equal(0, quota.UsedAmountToday);
        Assert.Equal(DateTime.UtcNow.Date, quota.LastUsageDate);
    }

    /// <summary>
    /// UTCID07 · Type A — estimatedTokens ÂM.
    /// DEFECT DFID001: code hiện tại không có guard cho giá trị âm.
    /// 50.000 + (−1.000) = 49.000 không &gt; 50.000 ⇒ trả về true, tức là cho phép tiêu thụ
    /// dù hạn mức đã cạn. Theo nghiệp vụ, số token ước lượng âm là dữ liệu rác và phải bị từ chối.
    /// Expected ghi theo hành vi ĐÚNG ⇒ test này DỰ KIẾN FAIL cho tới khi bổ sung guard.
    /// </summary>
    [Fact]
    public void UTCID07_NegativeEstimatedTokens_ShouldBeRejected()
    {
        var quota = BuildQuota(usedToday: DailyLimit);

        var result = quota.CanConsume(-1_000, out var warning);

        Assert.False(result);
        Assert.NotNull(warning);
    }
}
