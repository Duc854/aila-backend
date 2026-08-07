using AILA.Application.Common.Helpers;

namespace AILA.Application.Tests.UnitTests;

/// <summary>
/// Sheet: UT01_IsValidFormat — <see cref="EmailHelper.IsValidFormat"/>
/// Module: Authentication · CC = 4 · 8 test case
///
/// Nhánh: B1 = !IsNullOrWhiteSpace · B2 = Length &lt;= 254 · B3 = Regex.IsMatch
/// Toán tử &amp;&amp; short-circuit ⇒ B2 chỉ chạy khi B1 = T, B3 chỉ chạy khi B1 = B2 = T.
/// </summary>
public class UT01_EmailHelper_IsValidFormatTests
{
    /// <summary>Email hợp lệ dài đúng 254 ký tự: 'a'×242 + "@example.com" (12 ký tự).</summary>
    private static readonly string Email254 = new string('a', 242) + "@example.com";

    /// <summary>Email đúng định dạng nhưng dài 255 ký tự ⇒ vượt giới hạn.</summary>
    private static readonly string Email255 = new string('a', 243) + "@example.com";

    /// <summary>UTCID01 · B1=T, B2=T, B3=T · Type N — email hợp lệ thông thường.</summary>
    [Fact]
    public void UTCID01_ValidEmail_ReturnsTrue()
    {
        Assert.True(EmailHelper.IsValidFormat("user@aila.vn"));
    }

    /// <summary>UTCID02 · B1=F · Type A — null.</summary>
    [Fact]
    public void UTCID02_Null_ReturnsFalse()
    {
        Assert.False(EmailHelper.IsValidFormat(null!));
    }

    /// <summary>UTCID03 · B1=F · Type B — chuỗi rỗng (biên dưới).</summary>
    [Fact]
    public void UTCID03_EmptyString_ReturnsFalse()
    {
        Assert.False(EmailHelper.IsValidFormat(string.Empty));
    }

    /// <summary>UTCID04 · B1=F · Type A — toàn khoảng trắng.</summary>
    [Fact]
    public void UTCID04_WhitespaceOnly_ReturnsFalse()
    {
        Assert.False(EmailHelper.IsValidFormat("   "));
    }

    /// <summary>UTCID05 · B1=T, B2=T, B3=T · Type B — dài đúng 254 (biên trên hợp lệ).</summary>
    [Fact]
    public void UTCID05_LengthExactly254_ReturnsTrue()
    {
        Assert.Equal(254, Email254.Length);
        Assert.True(EmailHelper.IsValidFormat(Email254));
    }

    /// <summary>
    /// UTCID06 · B1=T, B2=F · Type B — dài 255 (biên trên không hợp lệ).
    /// Là test case DUY NHẤT phủ được nhánh B2 = F.
    /// </summary>
    [Fact]
    public void UTCID06_LengthExactly255_ReturnsFalse()
    {
        Assert.Equal(255, Email255.Length);
        Assert.False(EmailHelper.IsValidFormat(Email255));
    }

    /// <summary>UTCID07 · B1=T, B2=T, B3=F · Type A — thiếu ký tự '@'.</summary>
    [Fact]
    public void UTCID07_MissingAtSign_ReturnsFalse()
    {
        Assert.False(EmailHelper.IsValidFormat("useraila.vn"));
    }

    /// <summary>UTCID08 · B1=T, B2=T, B3=F · Type B — có '@' nhưng thiếu phần ".tld".</summary>
    [Fact]
    public void UTCID08_MissingTopLevelDomain_ReturnsFalse()
    {
        Assert.False(EmailHelper.IsValidFormat("a@b"));
    }
}
