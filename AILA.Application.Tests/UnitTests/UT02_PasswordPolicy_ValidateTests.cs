using AILA.Application.Common.Helpers;

namespace AILA.Application.Tests.UnitTests;

/// <summary>
/// Sheet: UT02_ValidatePassword — <see cref="PasswordPolicy.Validate"/>
/// Module: Authentication · CC = 3 · 6 test case
///
/// Nhánh: B1 = IsNullOrWhiteSpace(password) (return sớm) · B2 = password.Length &lt; 8
/// B1 = T return ngay ⇒ B2 chỉ chạm được khi password khác rỗng.
/// </summary>
public class UT02_PasswordPolicy_ValidateTests
{
    private const string EmptyMessage = "Mật khẩu mới không được để trống.";
    private const string TooShortMessage = "Mật khẩu phải có ít nhất 8 ký tự.";

    /// <summary>UTCID01 · B1=F, B2=F · Type N — mật khẩu 9 ký tự, hợp lệ.</summary>
    [Fact]
    public void UTCID01_ValidPassword_ReturnsNoViolation()
    {
        var violations = PasswordPolicy.Validate("Password1");

        Assert.Empty(violations);
    }

    /// <summary>UTCID02 · B1=T · Type A — null.</summary>
    [Fact]
    public void UTCID02_Null_ReturnsEmptyViolation()
    {
        var violations = PasswordPolicy.Validate(null);

        Assert.Single(violations);
        Assert.Equal(EmptyMessage, violations[0]);
    }

    /// <summary>UTCID03 · B1=T · Type B — chuỗi rỗng (biên dưới).</summary>
    [Fact]
    public void UTCID03_EmptyString_ReturnsEmptyViolation()
    {
        var violations = PasswordPolicy.Validate(string.Empty);

        Assert.Single(violations);
        Assert.Equal(EmptyMessage, violations[0]);
    }

    /// <summary>
    /// UTCID04 · B1=T · Type A — 8 khoảng trắng.
    /// Điểm white-box: ĐỦ 8 ký tự nhưng vẫn rơi vào B1 vì IsNullOrWhiteSpace bắt trước,
    /// KHÔNG rơi vào B2. Nếu chỉ đọc đặc tả "tối thiểu 8 ký tự" thì rất dễ ghi sai kỳ vọng.
    /// </summary>
    [Fact]
    public void UTCID04_WhitespaceOnlyWithLength8_ReturnsEmptyViolationNotLengthViolation()
    {
        var violations = PasswordPolicy.Validate("        ");

        Assert.Single(violations);
        Assert.Equal(EmptyMessage, violations[0]);
    }

    /// <summary>UTCID05 · B1=F, B2=T · Type B — 7 ký tự (biên dưới không hợp lệ).</summary>
    [Fact]
    public void UTCID05_LengthExactly7_ReturnsTooShortViolation()
    {
        var violations = PasswordPolicy.Validate("1234567");

        Assert.Single(violations);
        Assert.Equal(TooShortMessage, violations[0]);
    }

    /// <summary>UTCID06 · B1=F, B2=F · Type B — 8 ký tự (biên dưới hợp lệ).</summary>
    [Fact]
    public void UTCID06_LengthExactly8_ReturnsNoViolation()
    {
        var violations = PasswordPolicy.Validate("12345678");

        Assert.Empty(violations);
    }
}
