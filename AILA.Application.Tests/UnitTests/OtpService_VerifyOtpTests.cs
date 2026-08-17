using AILA.Infrastructure.Security;
using Microsoft.Extensions.Options;
using Shared.Models;

namespace AILA.Application.Tests.UnitTests;

/// <summary>
/// Sheet: UT03_VerifyOtp — <see cref="OtpService.VerifyOtp"/>
/// Module: Authentication · CC = 4 (5 đường thực thi) · 8 test case
///
/// Nhánh: B1 = IsNullOrEmpty(otp) · B2 = IsNullOrEmpty(expectedHash)
///        B3 = actual.Length != expected.Length · S8 = FixedTimeEquals(...)
/// B1 short-circuit che B2 ⇒ muốn phủ B2 thì otp phải hợp lệ.
/// B3 chỉ chạm được khi expectedHash khác 64 ký tự (HashOtp luôn trả hex 64).
/// </summary>
public class UT03_OtpService_VerifyOtpTests
{
    private const string EmailA = "user@aila.vn";
    private const string EmailB = "other@aila.vn";

    private readonly OtpService _sut;
    private readonly string _hashA6;
    private readonly string _hashB6;

    public UT03_OtpService_VerifyOtpTests()
    {
        var settings = new PasswordResetSettings
        {
            OtpLength = 6,
            OtpHashSecret = "aila-unit-test-otp-secret-key-32b"
        };

        _sut = new OtpService(Options.Create(settings));
        _hashA6 = _sut.HashOtp(EmailA, "123456");
        _hashB6 = _sut.HashOtp(EmailB, "123456");
    }

    /// <summary>UTCID01 · B1=F, B2=F, B3=F, S8=T · Type N — OTP đúng.</summary>
    [Fact]
    public void UTCID01_CorrectOtp_ReturnsTrue()
    {
        Assert.True(_sut.VerifyOtp(EmailA, "123456", _hashA6));
    }

    /// <summary>UTCID02 · B1=T · Type A — otp null.</summary>
    [Fact]
    public void UTCID02_NullOtp_ReturnsFalse()
    {
        Assert.False(_sut.VerifyOtp(EmailA, null!, _hashA6));
    }

    /// <summary>UTCID03 · B1=T · Type B — otp rỗng (biên dưới).</summary>
    [Fact]
    public void UTCID03_EmptyOtp_ReturnsFalse()
    {
        Assert.False(_sut.VerifyOtp(EmailA, string.Empty, _hashA6));
    }

    /// <summary>UTCID04 · B1=F, B2=T · Type A — expectedHash null. otp phải hợp lệ để thoát B1.</summary>
    [Fact]
    public void UTCID04_NullExpectedHash_ReturnsFalse()
    {
        Assert.False(_sut.VerifyOtp(EmailA, "123456", null!));
    }

    /// <summary>UTCID05 · B1=F, B2=T · Type B — expectedHash rỗng (biên dưới).</summary>
    [Fact]
    public void UTCID05_EmptyExpectedHash_ReturnsFalse()
    {
        Assert.False(_sut.VerifyOtp(EmailA, "123456", string.Empty));
    }

    /// <summary>UTCID06 · B1=F, B2=F, B3=F, S8=F · Type A — OTP sai (hash cùng độ dài, nội dung khác).</summary>
    [Fact]
    public void UTCID06_WrongOtp_ReturnsFalse()
    {
        Assert.False(_sut.VerifyOtp(EmailA, "654321", _hashA6));
    }

    /// <summary>
    /// UTCID07 · B1=F, B2=F, B3=F, S8=F · Type A
    /// Bất biến bảo mật: hash gắn với email ⇒ cùng OTP "123456" nhưng hash của email khác
    /// vẫn phải trả false (chống dùng chéo OTP giữa các tài khoản).
    /// </summary>
    [Fact]
    public void UTCID07_HashOfDifferentEmail_ReturnsFalse()
    {
        Assert.NotEqual(_hashA6, _hashB6);
        Assert.False(_sut.VerifyOtp(EmailA, "123456", _hashB6));
    }

    /// <summary>
    /// UTCID08 · B1=F, B2=F, B3=T · Type A — expectedHash dài 3 ký tự.
    /// Là test case DUY NHẤT phủ được nhánh B3 (chặn sớm trước khi FixedTimeEquals throw).
    /// </summary>
    [Fact]
    public void UTCID08_ExpectedHashWithDifferentLength_ReturnsFalse()
    {
        Assert.False(_sut.VerifyOtp(EmailA, "123456", "ABC"));
    }
}
