using AILA.Application.Common.Exceptions;
using AILA.Application.Common.Interfaces;
using AILA.Application.Common.Interfaces.Repositories;
using AILA.Application.Features.Authentication.Commands;
using AILA.Application.Features.Authentication.Commands.ConfirmPasswordReset;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Shared.Models;

namespace AILA.Application.Tests.UnitTests;

/// <summary>
/// Sheet: UT06_ConfirmPasswordReset — <see cref="ConfirmPasswordResetCommandHandler.Handle"/>
/// Module: Authentication · CC = 13 · 16 test case
///
/// Nhánh: B1 = token rỗng · B2 = confirm không khớp · B3 = có vi phạm policy
///        B4 = peek null · B5/B6 = user null / inactive
///        B7/B8/B9 = RejectSame &amp;&amp; có PasswordHash &amp;&amp; trùng mật khẩu cũ
///        B10/B11 = consume null / userId lệch · B12 = store throw
///
/// Bất biến quan trọng nhất (AC-7): VALIDATE XONG HẾT RỒI MỚI CONSUME TOKEN.
/// Mọi test case nhánh validate đều phải Verify Peek/Consume = Never.
/// </summary>
public class UT06_ConfirmPasswordReset_HandleTests
{
    private const string Token = "RESET-TOKEN-XYZ";
    private const string NewPassword = "NewPass123";

    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IPasswordResetStore> _store = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly PasswordResetSettings _settings = new()
    {
        RejectPasswordSameAsCurrent = true,
        OtpHashSecret = "aila-unit-test-otp-secret-key-32b"
    };

    public UT06_ConfirmPasswordReset_HandleTests()
    {
        _uow.SetupGet(x => x.Users).Returns(_users.Object);
        _uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _hasher.Setup(x => x.HashPassword(It.IsAny<string>())).Returns<string>(p => $"HASH({p})");
        _hasher.Setup(x => x.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(false);
    }

    private ConfirmPasswordResetCommandHandler CreateSut() => new(
        _uow.Object,
        _store.Object,
        _hasher.Object,
        Options.Create(_settings),
        Mock.Of<ILogger<ConfirmPasswordResetCommandHandler>>());

    private static User UserWithPassword() =>
        new("user@aila.vn", "Nguyen Van A", UserRole.Learner, passwordHash: "OLD_HASH");

    private static User UserWithGoogleOnly() =>
        new("user@aila.vn", "Nguyen Van A", UserRole.Learner, googleId: "google-sub-123");

    private static User InactiveUser()
    {
        var user = UserWithPassword();
        user.Deactivate();
        return user;
    }

    private User SetupTokenResolvesTo(User user, Guid? consumedUserId = null)
    {
        _store.Setup(x => x.PeekResetTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(user.Id);
        _users.Setup(x => x.GetByIdAsync(user.Id)).ReturnsAsync(user);
        _store.Setup(x => x.ConsumeResetTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(consumedUserId ?? user.Id);
        return user;
    }

    private void AssertTokenUntouched()
    {
        _store.Verify(x => x.PeekResetTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _store.Verify(x => x.ConsumeResetTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>UTCID01 · B1=T · Type A — ResetToken null, token chưa bị đụng tới.</summary>
    [Fact]
    public async Task UTCID01_NullResetToken_ReturnsValidationError()
    {
        var result = await CreateSut().Handle(
            new ConfirmPasswordResetCommand(null!, NewPassword, NewPassword), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(PasswordResetErrorCodes.ValidationError, result.ErrorCode);
        AssertTokenUntouched();
    }

    /// <summary>UTCID02 · B1=T · Type A — ResetToken toàn khoảng trắng.</summary>
    [Fact]
    public async Task UTCID02_WhitespaceResetToken_ReturnsValidationError()
    {
        var result = await CreateSut().Handle(
            new ConfirmPasswordResetCommand("   ", NewPassword, NewPassword), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(PasswordResetErrorCodes.ValidationError, result.ErrorCode);
        AssertTokenUntouched();
    }

    /// <summary>
    /// UTCID03 · B2=F, B3=T · Type B — mật khẩu 5 ký tự.
    /// AC-7: password sai policy KHÔNG được đốt token.
    /// </summary>
    [Fact]
    public async Task UTCID03_PasswordTooShort_ReturnsInvalidPasswordWithoutTouchingToken()
    {
        var result = await CreateSut().Handle(
            new ConfirmPasswordResetCommand(Token, "short", "short"), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(PasswordResetErrorCodes.InvalidPassword, result.ErrorCode);
        Assert.Equal("Mật khẩu phải có ít nhất 8 ký tự.", result.ErrorMessage);
        AssertTokenUntouched();
    }

    /// <summary>UTCID04 · B2=T, B3=T · Type A — mật khẩu xác nhận không khớp.</summary>
    [Fact]
    public async Task UTCID04_ConfirmPasswordMismatch_ReturnsInvalidPassword()
    {
        var result = await CreateSut().Handle(
            new ConfirmPasswordResetCommand(Token, NewPassword, "NewPass124"), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(PasswordResetErrorCodes.InvalidPassword, result.ErrorCode);
        Assert.Equal("Mật khẩu xác nhận không khớp với mật khẩu mới.", result.ErrorMessage);
        AssertTokenUntouched();
    }

    /// <summary>
    /// UTCID05 · B2=T, B3=T · Type A — vi phạm cả policy lẫn confirm.
    /// AF-03: gom mọi vi phạm vào một lần trả về, nối bằng dấu cách.
    /// </summary>
    [Fact]
    public async Task UTCID05_BothPolicyAndConfirmViolations_ReturnsAllMessagesJoined()
    {
        var result = await CreateSut().Handle(
            new ConfirmPasswordResetCommand(Token, "short", "other"), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(
            "Mật khẩu phải có ít nhất 8 ký tự. Mật khẩu xác nhận không khớp với mật khẩu mới.",
            result.ErrorMessage);
        AssertTokenUntouched();
    }

    /// <summary>UTCID06 · B3=F, B4=T · Type A — token không tồn tại / đã hết hạn.</summary>
    [Fact]
    public async Task UTCID06_TokenNotFound_ReturnsInvalidOrExpiredToken()
    {
        _store.Setup(x => x.PeekResetTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync((Guid?)null);

        var result = await CreateSut().Handle(
            new ConfirmPasswordResetCommand(Token, NewPassword, NewPassword), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(PasswordResetErrorCodes.InvalidOrExpiredToken, result.ErrorCode);
        _store.Verify(x => x.ConsumeResetTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>UTCID07 · B5=T · Type A — token trỏ tới user không tồn tại ⇒ dọn token.</summary>
    [Fact]
    public async Task UTCID07_UserNotFound_ConsumesTokenAndReturnsInvalidToken()
    {
        var userId = Guid.NewGuid();
        _store.Setup(x => x.PeekResetTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(userId);
        _users.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync((User?)null);

        var result = await CreateSut().Handle(
            new ConfirmPasswordResetCommand(Token, NewPassword, NewPassword), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(PasswordResetErrorCodes.InvalidOrExpiredToken, result.ErrorCode);
        _store.Verify(x => x.ConsumeResetTokenAsync(Token, It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>UTCID08 · B5=F, B6=T · Type A — tài khoản bị vô hiệu hoá ⇒ dọn token.</summary>
    [Fact]
    public async Task UTCID08_InactiveUser_ConsumesTokenAndReturnsInvalidToken()
    {
        SetupTokenResolvesTo(InactiveUser());

        var result = await CreateSut().Handle(
            new ConfirmPasswordResetCommand(Token, NewPassword, NewPassword), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(PasswordResetErrorCodes.InvalidOrExpiredToken, result.ErrorCode);
        _store.Verify(x => x.ConsumeResetTokenAsync(Token, It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// UTCID09 · B7=T, B8=T, B9=T · Type A — mật khẩu mới trùng mật khẩu hiện tại.
    /// EDGE-09: từ chối nhưng GIỮ NGUYÊN token để người dùng thử lại (Consume = Never).
    /// </summary>
    [Fact]
    public async Task UTCID09_NewPasswordSameAsCurrent_ReturnsPasswordReusedAndKeepsToken()
    {
        var user = SetupTokenResolvesTo(UserWithPassword());
        _hasher.Setup(x => x.Verify(NewPassword, user.PasswordHash!)).Returns(true);

        var result = await CreateSut().Handle(
            new ConfirmPasswordResetCommand(Token, NewPassword, NewPassword), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(PasswordResetErrorCodes.PasswordReused, result.ErrorCode);
        _store.Verify(x => x.ConsumeResetTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>UTCID10 · B7=F · Type N — tắt cấu hình RejectPasswordSameAsCurrent ⇒ vẫn cho đổi.</summary>
    [Fact]
    public async Task UTCID10_RejectSameDisabled_AllowsSamePassword()
    {
        _settings.RejectPasswordSameAsCurrent = false;
        var user = SetupTokenResolvesTo(UserWithPassword());
        _hasher.Setup(x => x.Verify(NewPassword, user.PasswordHash!)).Returns(true);

        var result = await CreateSut().Handle(
            new ConfirmPasswordResetCommand(Token, NewPassword, NewPassword), CancellationToken.None);

        Assert.True(result.Success);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// UTCID11 · B7=T, B8=F · Type B — tài khoản Google (PasswordHash = null).
    /// Short-circuit ⇒ hasher.Verify KHÔNG được gọi (nếu gọi sẽ truyền null vào tham số non-null).
    /// </summary>
    [Fact]
    public async Task UTCID11_UserWithoutPasswordHash_SkipsReuseCheck()
    {
        SetupTokenResolvesTo(UserWithGoogleOnly());

        var result = await CreateSut().Handle(
            new ConfirmPasswordResetCommand(Token, NewPassword, NewPassword), CancellationToken.None);

        Assert.True(result.Success);
        _hasher.Verify(x => x.Verify(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>UTCID12 · B10=T · Type A — token bị request khác tiêu thụ mất (EDGE-10).</summary>
    [Fact]
    public async Task UTCID12_ConsumeReturnsNull_ReturnsInvalidTokenWithoutSaving()
    {
        var user = UserWithPassword();
        _store.Setup(x => x.PeekResetTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(user.Id);
        _users.Setup(x => x.GetByIdAsync(user.Id)).ReturnsAsync(user);
        _store.Setup(x => x.ConsumeResetTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync((Guid?)null);

        var result = await CreateSut().Handle(
            new ConfirmPasswordResetCommand(Token, NewPassword, NewPassword), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(PasswordResetErrorCodes.InvalidOrExpiredToken, result.ErrorCode);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>UTCID13 · B10=F, B11=T · Type A — consume trả về userId khác (EDGE-12 race condition).</summary>
    [Fact]
    public async Task UTCID13_ConsumeReturnsDifferentUserId_ReturnsInvalidTokenWithoutSaving()
    {
        SetupTokenResolvesTo(UserWithPassword(), consumedUserId: Guid.NewGuid());

        var result = await CreateSut().Handle(
            new ConfirmPasswordResetCommand(Token, NewPassword, NewPassword), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(PasswordResetErrorCodes.InvalidOrExpiredToken, result.ErrorCode);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// UTCID14 · B11=F · Type N — luồng thành công. Token đầu vào có khoảng trắng thừa ⇒ phải được Trim.
    /// </summary>
    [Fact]
    public async Task UTCID14_HappyPath_UpdatesPasswordAndSaves()
    {
        var user = SetupTokenResolvesTo(UserWithPassword());

        var result = await CreateSut().Handle(
            new ConfirmPasswordResetCommand($"  {Token}  ", NewPassword, NewPassword), CancellationToken.None);

        Assert.True(result.Success);
        Assert.Null(result.Data);
        _store.Verify(x => x.PeekResetTokenAsync(Token, It.IsAny<CancellationToken>()), Times.Once);
        _hasher.Verify(x => x.HashPassword(NewPassword), Times.Once);
        Assert.Equal($"HASH({NewPassword})", user.PasswordHash);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>UTCID15 · B3=F · Type B — mật khẩu dài đúng 8 ký tự (biên dưới hợp lệ).</summary>
    [Fact]
    public async Task UTCID15_PasswordLengthExactly8_Succeeds()
    {
        var user = SetupTokenResolvesTo(UserWithPassword());

        var result = await CreateSut().Handle(
            new ConfirmPasswordResetCommand(Token, "Pass1234", "Pass1234"), CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal("HASH(Pass1234)", user.PasswordHash);
    }

    /// <summary>UTCID16 · B12=T · Type A — store không sẵn sàng ⇒ 503, không đổi mật khẩu.</summary>
    [Fact]
    public async Task UTCID16_StoreUnavailable_ReturnsServiceUnavailable()
    {
        _store.Setup(x => x.PeekResetTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
              .ThrowsAsync(new PasswordResetStoreUnavailableException("redis down"));

        var result = await CreateSut().Handle(
            new ConfirmPasswordResetCommand(Token, NewPassword, NewPassword), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(PasswordResetErrorCodes.ServiceUnavailable, result.ErrorCode);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
