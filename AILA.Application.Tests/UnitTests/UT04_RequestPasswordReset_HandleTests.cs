using System.Diagnostics;
using AILA.Application.Common.Exceptions;
using AILA.Application.Common.Interfaces;
using AILA.Application.Common.Interfaces.Repositories;
using AILA.Application.Features.Authentication.Commands;
using AILA.Application.Features.Authentication.Commands.RequestPasswordReset;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Shared.Models;

namespace AILA.Application.Tests.UnitTests;

/// <summary>
/// Sheet: UT04_RequestPasswordReset — <see cref="RequestPasswordResetCommandHandler.Handle"/>
/// Module: Authentication · CC = 10 · 12 test case
///
/// Nhánh: B1 = Email rỗng · B2 = email sai định dạng · B3 = có IpAddress
///        B4 = vượt rate limit IP · B5 = vượt rate limit email
///        B6/B7 = user null / không active · B8 = store throw · B9 = padding &gt; 0
///
/// Điểm mấu chốt: nhánh "email không khả dụng" (B6/B7) trả về Success GIỐNG HỆT nhánh
/// thành công ⇒ phải phân biệt bằng Verify trên mock, không thể bằng giá trị trả về.
/// </summary>
public class UT04_RequestPasswordReset_HandleTests
{
    private const string ValidEmail = "user@aila.vn";
    private const string ValidIp = "1.2.3.4";

    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IPasswordResetStore> _store = new();
    private readonly Mock<IOtpService> _otpService = new();
    private readonly Mock<IEmailSender> _emailSender = new();
    private readonly PasswordResetSettings _settings = new()
    {
        OtpLength = 6,
        OtpTtlSeconds = 300,
        MaxOtpRequestsPerEmail = 5,
        MaxOtpRequestsPerIp = 20,
        MinRequestDurationMs = 0,
        OtpHashSecret = "aila-unit-test-otp-secret-key-32b"
    };

    public UT04_RequestPasswordReset_HandleTests()
    {
        _uow.SetupGet(x => x.Users).Returns(_users.Object);
        _otpService.Setup(x => x.GenerateOtp()).Returns("123456");
        _otpService.Setup(x => x.HashOtp(It.IsAny<string>(), It.IsAny<string>())).Returns("HASHED_OTP");
        _store.Setup(x => x.IncrementRateLimitAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(1L);
    }

    private RequestPasswordResetCommandHandler CreateSut() => new(
        _uow.Object,
        _store.Object,
        _otpService.Object,
        _emailSender.Object,
        Options.Create(_settings),
        Mock.Of<ILogger<RequestPasswordResetCommandHandler>>());

    private static User ActiveUser() =>
        new(ValidEmail, "Nguyen Van A", UserRole.Learner, passwordHash: "OLD_HASH");

    private static User InactiveUser()
    {
        var user = ActiveUser();
        user.Deactivate();
        return user;
    }

    private void SetupIpCount(long count) =>
        _store.Setup(x => x.IncrementRateLimitAsync("ip", It.IsAny<string>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(count);

    private void SetupEmailCount(long count) =>
        _store.Setup(x => x.IncrementRateLimitAsync("email", It.IsAny<string>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(count);

    private void SetupUser(User? user) =>
        _users.Setup(x => x.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);

    private void AssertNoOtpGenerated()
    {
        _otpService.Verify(x => x.GenerateOtp(), Times.Never);
        _store.Verify(x => x.SaveOtpAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _emailSender.Verify(x => x.SendPasswordResetOtpAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>UTCID01 · B1=T · Type A — Email null.</summary>
    [Fact]
    public async Task UTCID01_NullEmail_ReturnsValidationError()
    {
        var result = await CreateSut().Handle(new RequestPasswordResetCommand(null!, ValidIp), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(PasswordResetErrorCodes.ValidationError, result.ErrorCode);
        _store.Verify(x => x.IncrementRateLimitAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>UTCID02 · B1=T · Type A — Email toàn khoảng trắng.</summary>
    [Fact]
    public async Task UTCID02_WhitespaceEmail_ReturnsValidationError()
    {
        var result = await CreateSut().Handle(new RequestPasswordResetCommand("   ", null), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(PasswordResetErrorCodes.ValidationError, result.ErrorCode);
        _store.Verify(x => x.IncrementRateLimitAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>UTCID03 · B1=F, B2=T · Type A — email sai định dạng, chặn trước khi chạm store.</summary>
    [Fact]
    public async Task UTCID03_InvalidEmailFormat_ReturnsInvalidEmailFormat()
    {
        var result = await CreateSut().Handle(new RequestPasswordResetCommand("useraila.vn", null), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(PasswordResetErrorCodes.InvalidEmailFormat, result.ErrorCode);
        _store.Verify(x => x.IncrementRateLimitAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>UTCID04 · B3=T, B4=T · Type A — vượt rate limit theo IP (21 &gt; 20).</summary>
    [Fact]
    public async Task UTCID04_IpRateLimitExceeded_ReturnsRateLimitExceeded()
    {
        SetupIpCount(21);

        var result = await CreateSut().Handle(new RequestPasswordResetCommand(ValidEmail, ValidIp), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(PasswordResetErrorCodes.RateLimitExceeded, result.ErrorCode);
        _store.Verify(x => x.IncrementRateLimitAsync("email", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        AssertNoOtpGenerated();
    }

    /// <summary>UTCID05 · B4=F · Type B — ipCount đúng bằng ngưỡng 20 ⇒ vẫn cho qua (toán tử là &gt;).</summary>
    [Fact]
    public async Task UTCID05_IpCountExactlyAtLimit_Proceeds()
    {
        SetupIpCount(20);
        SetupEmailCount(1);
        SetupUser(ActiveUser());

        var result = await CreateSut().Handle(new RequestPasswordResetCommand(ValidEmail, ValidIp), CancellationToken.None);

        Assert.True(result.Success);
        _store.Verify(x => x.SaveOtpAsync(ValidEmail, "HASHED_OTP", It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>UTCID06 · B3=F, B5=T · Type A — không có IP, vượt rate limit theo email (6 &gt; 5).</summary>
    [Fact]
    public async Task UTCID06_EmailRateLimitExceeded_ReturnsRateLimitExceeded()
    {
        SetupEmailCount(6);

        var result = await CreateSut().Handle(new RequestPasswordResetCommand(ValidEmail, null), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(PasswordResetErrorCodes.RateLimitExceeded, result.ErrorCode);
        _store.Verify(x => x.IncrementRateLimitAsync("ip", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        AssertNoOtpGenerated();
    }

    /// <summary>UTCID07 · B5=F · Type B — emailCount đúng bằng ngưỡng 5 ⇒ vẫn cho qua.</summary>
    [Fact]
    public async Task UTCID07_EmailCountExactlyAtLimit_Proceeds()
    {
        SetupEmailCount(5);
        SetupUser(ActiveUser());

        var result = await CreateSut().Handle(new RequestPasswordResetCommand(ValidEmail, null), CancellationToken.None);

        Assert.True(result.Success);
        _store.Verify(x => x.SaveOtpAsync(ValidEmail, "HASHED_OTP", It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// UTCID08 · B6=T · Type A — email không tồn tại.
    /// Response TRUNG TÍNH (Success = true) ⇒ chỉ phân biệt được bằng Verify: không sinh OTP, không gửi mail.
    /// </summary>
    [Fact]
    public async Task UTCID08_UserNotFound_ReturnsNeutralSuccessWithoutSendingOtp()
    {
        SetupUser(null);

        var result = await CreateSut().Handle(new RequestPasswordResetCommand(ValidEmail, ValidIp), CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(300, result.Data!.OtpExpiresInSeconds);
        AssertNoOtpGenerated();
    }

    /// <summary>UTCID09 · B6=F, B7=T · Type A — tài khoản bị vô hiệu hoá, cũng trả response trung tính.</summary>
    [Fact]
    public async Task UTCID09_InactiveUser_ReturnsNeutralSuccessWithoutSendingOtp()
    {
        SetupUser(InactiveUser());

        var result = await CreateSut().Handle(new RequestPasswordResetCommand(ValidEmail, ValidIp), CancellationToken.None);

        Assert.True(result.Success);
        AssertNoOtpGenerated();
    }

    /// <summary>
    /// UTCID10 · Toàn bộ nhánh = F · Type N — luồng thành công đầy đủ.
    /// Kiểm tra thêm: email được chuẩn hoá (trim + lowercase) trước khi làm key,
    /// và TTL truyền sang Email Service là số PHÚT (300 / 60 = 5).
    /// </summary>
    [Fact]
    public async Task UTCID10_HappyPath_GeneratesOtpAndEnqueuesEmail()
    {
        var user = ActiveUser();
        SetupUser(user);

        var result = await CreateSut().Handle(
            new RequestPasswordResetCommand("  User@AILA.vn  ", ValidIp), CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(300, result.Data!.OtpExpiresInSeconds);

        _store.Verify(x => x.IncrementRateLimitAsync("email", ValidEmail, It.IsAny<CancellationToken>()), Times.Once);
        _otpService.Verify(x => x.HashOtp(ValidEmail, "123456"), Times.Once);
        _store.Verify(x => x.SaveOtpAsync(ValidEmail, "HASHED_OTP", It.IsAny<CancellationToken>()), Times.Once);
        _emailSender.Verify(x => x.SendPasswordResetOtpAsync(
            user.Email, user.FullName, "123456", 5, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>UTCID11 · B8=T · Type A — store không sẵn sàng ⇒ 503, tuyệt đối không sinh OTP.</summary>
    [Fact]
    public async Task UTCID11_StoreUnavailable_ReturnsServiceUnavailable()
    {
        _store.Setup(x => x.IncrementRateLimitAsync("ip", It.IsAny<string>(), It.IsAny<CancellationToken>()))
              .ThrowsAsync(new PasswordResetStoreUnavailableException("redis down"));

        var result = await CreateSut().Handle(new RequestPasswordResetCommand(ValidEmail, ValidIp), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(PasswordResetErrorCodes.ServiceUnavailable, result.ErrorCode);
        AssertNoOtpGenerated();
    }

    /// <summary>
    /// UTCID12 · B9=T · Type B — MinRequestDurationMs = 200 ⇒ response bị đệm cho đủ 200 ms
    /// (chống user enumeration qua timing).
    /// </summary>
    [Fact]
    public async Task UTCID12_ResponseTimeIsPaddedToMinimumDuration()
    {
        _settings.MinRequestDurationMs = 200;
        SetupEmailCount(1);
        SetupUser(null);

        var stopwatch = Stopwatch.StartNew();
        var result = await CreateSut().Handle(new RequestPasswordResetCommand(ValidEmail, null), CancellationToken.None);
        stopwatch.Stop();

        Assert.True(result.Success);
        Assert.True(stopwatch.ElapsedMilliseconds >= 190,
            $"Kỳ vọng response được đệm tối thiểu ~200 ms, thực tế {stopwatch.ElapsedMilliseconds} ms.");
    }
}
