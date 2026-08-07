using AILA.Application.Common.Exceptions;
using AILA.Application.Common.Interfaces;
using AILA.Application.Common.Interfaces.Repositories;
using AILA.Application.Common.Models;
using AILA.Application.Features.Authentication.Commands;
using AILA.Application.Features.Authentication.Commands.VerifyPasswordResetOtp;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Shared.Models;

namespace AILA.Application.Tests.UnitTests;

/// <summary>
/// Sheet: UT05_VerifyOtpHandler — <see cref="VerifyPasswordResetOtpCommandHandler.Handle"/>
/// Module: Authentication · CC = 10 · 11 test case
///
/// Nhánh: B1/B2 = Email/Otp rỗng · B3 = email sai định dạng · B4 = không còn OTP active
///        B5 = OTP sai · B6 = vượt ngưỡng thử sai · B7/B8 = user null / inactive · B9 = store throw
///
/// Điểm mấu chốt: 6/11 test case trả về CÙNG một ErrorCode INVALID_OR_EXPIRED_OTP (AF-02 —
/// không tiết lộ lý do). Phân biệt đường thực thi hoàn toàn bằng Verify trên mock.
/// </summary>
public class UT05_VerifyPasswordResetOtp_HandleTests
{
    private const string ValidEmail = "user@aila.vn";
    private static readonly OtpEntry Entry = new("HASHED_OTP", 0);

    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IPasswordResetStore> _store = new();
    private readonly Mock<IOtpService> _otpService = new();
    private readonly PasswordResetSettings _settings = new()
    {
        MaxVerifyAttempts = 5,
        ResetTokenTtlSeconds = 600,
        OtpHashSecret = "aila-unit-test-otp-secret-key-32b"
    };

    public UT05_VerifyPasswordResetOtp_HandleTests()
    {
        _uow.SetupGet(x => x.Users).Returns(_users.Object);
        _otpService.Setup(x => x.GenerateResetToken()).Returns("RESET-TOKEN-XYZ");
    }

    private VerifyPasswordResetOtpCommandHandler CreateSut() => new(
        _uow.Object,
        _store.Object,
        _otpService.Object,
        Options.Create(_settings),
        Mock.Of<ILogger<VerifyPasswordResetOtpCommandHandler>>());

    private static User ActiveUser() =>
        new(ValidEmail, "Nguyen Van A", UserRole.Learner, passwordHash: "OLD_HASH");

    private static User InactiveUser()
    {
        var user = ActiveUser();
        user.Deactivate();
        return user;
    }

    private void SetupOtpEntry(OtpEntry? entry) =>
        _store.Setup(x => x.GetOtpAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(entry);

    private void SetupVerify(bool result) =>
        _otpService.Setup(x => x.VerifyOtp(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(result);

    private void SetupAttempts(int attempts) =>
        _store.Setup(x => x.IncrementOtpAttemptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(attempts);

    private void SetupUser(User? user) =>
        _users.Setup(x => x.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);

    /// <summary>UTCID01 · B1=T · Type A — Email null.</summary>
    [Fact]
    public async Task UTCID01_NullEmail_ReturnsValidationError()
    {
        var result = await CreateSut().Handle(new VerifyPasswordResetOtpCommand(null!, "123456"), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(PasswordResetErrorCodes.ValidationError, result.ErrorCode);
        _store.Verify(x => x.GetOtpAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>UTCID02 · B1=F, B2=T · Type A — Otp rỗng. Email phải hợp lệ để thoát B1.</summary>
    [Fact]
    public async Task UTCID02_EmptyOtp_ReturnsValidationError()
    {
        var result = await CreateSut().Handle(new VerifyPasswordResetOtpCommand(ValidEmail, ""), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(PasswordResetErrorCodes.ValidationError, result.ErrorCode);
        _store.Verify(x => x.GetOtpAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>UTCID03 · B3=T · Type A — email sai định dạng.</summary>
    [Fact]
    public async Task UTCID03_InvalidEmailFormat_ReturnsInvalidEmailFormat()
    {
        var result = await CreateSut().Handle(new VerifyPasswordResetOtpCommand("useraila.vn", "123456"), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(PasswordResetErrorCodes.InvalidEmailFormat, result.ErrorCode);
        _store.Verify(x => x.GetOtpAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>UTCID04 · B4=T · Type A — OTP đã hết hạn hoặc đã dùng (không còn bản ghi).</summary>
    [Fact]
    public async Task UTCID04_NoActiveOtp_ReturnsInvalidOrExpiredOtp()
    {
        SetupOtpEntry(null);

        var result = await CreateSut().Handle(new VerifyPasswordResetOtpCommand(ValidEmail, "123456"), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(PasswordResetErrorCodes.InvalidOrExpiredOtp, result.ErrorCode);
        _store.Verify(x => x.IncrementOtpAttemptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _store.Verify(x => x.DeleteOtpAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>UTCID05 · B5=T, B6=F · Type A — OTP sai lần đầu: chỉ tăng bộ đếm, không huỷ OTP.</summary>
    [Fact]
    public async Task UTCID05_WrongOtpFirstAttempt_IncrementsCounterWithoutDeletingOtp()
    {
        SetupOtpEntry(Entry);
        SetupVerify(false);
        SetupAttempts(1);

        var result = await CreateSut().Handle(new VerifyPasswordResetOtpCommand(ValidEmail, "999999"), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(PasswordResetErrorCodes.InvalidOrExpiredOtp, result.ErrorCode);
        _store.Verify(x => x.IncrementOtpAttemptAsync(ValidEmail, It.IsAny<CancellationToken>()), Times.Once);
        _store.Verify(x => x.DeleteOtpAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>UTCID06 · B6=F · Type B — attempts = 4 (đúng ngưỡng − 1) ⇒ CHƯA huỷ OTP.</summary>
    [Fact]
    public async Task UTCID06_AttemptsJustBelowThreshold_DoesNotDeleteOtp()
    {
        SetupOtpEntry(Entry);
        SetupVerify(false);
        SetupAttempts(4);

        var result = await CreateSut().Handle(new VerifyPasswordResetOtpCommand(ValidEmail, "999999"), CancellationToken.None);

        Assert.False(result.Success);
        _store.Verify(x => x.DeleteOtpAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>UTCID07 · B6=T · Type B — attempts = 5 (đúng ngưỡng) ⇒ huỷ OTP, buộc xin mã mới.</summary>
    [Fact]
    public async Task UTCID07_AttemptsReachThreshold_DeletesOtp()
    {
        SetupOtpEntry(Entry);
        SetupVerify(false);
        SetupAttempts(5);

        var result = await CreateSut().Handle(new VerifyPasswordResetOtpCommand(ValidEmail, "999999"), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(PasswordResetErrorCodes.InvalidOrExpiredOtp, result.ErrorCode);
        _store.Verify(x => x.DeleteOtpAsync(ValidEmail, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// UTCID08 · B7=T · Type A — OTP đúng nhưng user không tồn tại.
    /// OTP ĐÃ bị xoá (chống replay) nhưng KHÔNG được cấp reset token.
    /// </summary>
    [Fact]
    public async Task UTCID08_CorrectOtpButUserNotFound_DeletesOtpAndDoesNotIssueToken()
    {
        SetupOtpEntry(Entry);
        SetupVerify(true);
        SetupUser(null);

        var result = await CreateSut().Handle(new VerifyPasswordResetOtpCommand(ValidEmail, "123456"), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(PasswordResetErrorCodes.InvalidOrExpiredOtp, result.ErrorCode);
        _store.Verify(x => x.DeleteOtpAsync(ValidEmail, It.IsAny<CancellationToken>()), Times.Once);
        _store.Verify(x => x.SaveResetTokenAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>UTCID09 · B7=F, B8=T · Type A — OTP đúng nhưng tài khoản bị vô hiệu hoá.</summary>
    [Fact]
    public async Task UTCID09_CorrectOtpButInactiveUser_DoesNotIssueToken()
    {
        SetupOtpEntry(Entry);
        SetupVerify(true);
        SetupUser(InactiveUser());

        var result = await CreateSut().Handle(new VerifyPasswordResetOtpCommand(ValidEmail, "123456"), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(PasswordResetErrorCodes.InvalidOrExpiredOtp, result.ErrorCode);
        _store.Verify(x => x.SaveResetTokenAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// UTCID10 · Toàn bộ nhánh lỗi = F · Type N — luồng thành công.
    /// Kiểm tra thêm: email được chuẩn hoá, OTP được Trim trước khi verify.
    /// </summary>
    [Fact]
    public async Task UTCID10_HappyPath_DeletesOtpAndIssuesResetToken()
    {
        var user = ActiveUser();
        SetupOtpEntry(Entry);
        SetupVerify(true);
        SetupUser(user);

        var result = await CreateSut().Handle(
            new VerifyPasswordResetOtpCommand("  User@AILA.vn ", " 123456 "), CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal("RESET-TOKEN-XYZ", result.Data!.ResetToken);
        Assert.Equal(600, result.Data.ExpiresInSeconds);

        _otpService.Verify(x => x.VerifyOtp(ValidEmail, "123456", "HASHED_OTP"), Times.Once);
        _store.Verify(x => x.DeleteOtpAsync(ValidEmail, It.IsAny<CancellationToken>()), Times.Once);
        _store.Verify(x => x.SaveResetTokenAsync("RESET-TOKEN-XYZ", user.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>UTCID11 · B9=T · Type A — store không sẵn sàng ⇒ 503, không cho đi tiếp.</summary>
    [Fact]
    public async Task UTCID11_StoreUnavailable_ReturnsServiceUnavailable()
    {
        _store.Setup(x => x.GetOtpAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
              .ThrowsAsync(new PasswordResetStoreUnavailableException("redis down"));

        var result = await CreateSut().Handle(new VerifyPasswordResetOtpCommand(ValidEmail, "123456"), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(PasswordResetErrorCodes.ServiceUnavailable, result.ErrorCode);
        _store.Verify(x => x.SaveResetTokenAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
