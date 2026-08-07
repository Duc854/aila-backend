using AILA.Application.Common.Interfaces;
using AILA.Application.Common.Interfaces.Repositories;
using AILA.Application.Features.Profile.Commands.ChangePassword;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using Moq;

namespace AILA.Application.Tests.UnitTests;

/// <summary>
/// Sheet: UT07_ChangePassword — <see cref="ChangePasswordCommandHandler.Handle"/>
/// Module: Profile · CC = 9 · 12 test case
///
/// Nhánh: B1 = NewPassword rỗng · B2 = độ dài &lt; 8 · B3 = user null · B4 = user inactive
///        B5 = đã có PasswordHash · B6 = CurrentPassword rỗng
///        B7 = mật khẩu hiện tại sai · B8 = mật khẩu mới trùng mật khẩu cũ
///
/// B5 là điều kiện chắn cho toàn bộ B6/B7/B8: nếu mọi test đều dùng tài khoản Google
/// (PasswordHash = null) thì ba nhánh này KHÔNG BAO GIỜ được thực thi.
/// </summary>
public class UT07_ChangePassword_HandleTests
{
    private const string CurrentPassword = "Old12345";
    private const string NewPassword = "NewPassword123";

    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IGenericRepository<User>> _userRepo = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Guid _userId = Guid.NewGuid();

    public UT07_ChangePassword_HandleTests()
    {
        _uow.Setup(x => x.Repository<User>()).Returns(_userRepo.Object);
        _uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _hasher.Setup(x => x.HashPassword(It.IsAny<string>())).Returns<string>(p => $"HASH({p})");
        _hasher.Setup(x => x.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(false);
    }

    private ChangePasswordCommandHandler CreateSut() => new(_uow.Object, _hasher.Object);

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

    private User SetupUser(User? user)
    {
        _userRepo.Setup(x => x.GetByIdAsync(_userId)).ReturnsAsync(user);
        return user!;
    }

    /// <summary>UTCID01 · B1=T · Type A — NewPassword null.</summary>
    [Fact]
    public async Task UTCID01_NullNewPassword_ReturnsValidationError()
    {
        var result = await CreateSut().Handle(
            new ChangePasswordCommand(_userId, CurrentPassword, null!), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("VALIDATION_ERROR", result.ErrorCode);
        Assert.Equal("Mật khẩu mới không được để trống.", result.ErrorMessage);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// UTCID02 · B1=T · Type A — 7 khoảng trắng.
    /// Điểm white-box: IsNullOrWhiteSpace bắt trước IsPasswordStrong, nên thông điệp là
    /// "không được để trống" chứ KHÔNG phải "ít nhất 8 ký tự".
    /// </summary>
    [Fact]
    public async Task UTCID02_WhitespaceNewPassword_ReturnsEmptyMessageNotLengthMessage()
    {
        var result = await CreateSut().Handle(
            new ChangePasswordCommand(_userId, CurrentPassword, "       "), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Mật khẩu mới không được để trống.", result.ErrorMessage);
    }

    /// <summary>UTCID03 · B1=F, B2=T · Type B — 7 ký tự (biên dưới không hợp lệ).</summary>
    [Fact]
    public async Task UTCID03_NewPasswordLength7_ReturnsValidationError()
    {
        var result = await CreateSut().Handle(
            new ChangePasswordCommand(_userId, CurrentPassword, "1234567"), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Mật khẩu mới phải có ít nhất 8 ký tự.", result.ErrorMessage);
        _userRepo.Verify(x => x.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    /// <summary>UTCID04 · B3=T · Type A — không tìm thấy người dùng.</summary>
    [Fact]
    public async Task UTCID04_UserNotFound_ReturnsUserNotFound()
    {
        SetupUser(null);

        var result = await CreateSut().Handle(
            new ChangePasswordCommand(_userId, CurrentPassword, NewPassword), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("USER_NOT_FOUND", result.ErrorCode);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>UTCID05 · B4=T · Type A — tài khoản bị vô hiệu hoá.</summary>
    [Fact]
    public async Task UTCID05_InactiveUser_ReturnsAccountInactive()
    {
        SetupUser(InactiveUser());

        var result = await CreateSut().Handle(
            new ChangePasswordCommand(_userId, CurrentPassword, NewPassword), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("ACCOUNT_INACTIVE", result.ErrorCode);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// UTCID06 · B5=F · Type N — tài khoản Google đặt mật khẩu lần đầu.
    /// Nhánh đặc quyền: bỏ qua toàn bộ kiểm tra mật khẩu cũ ⇒ hasher.Verify KHÔNG được gọi.
    /// </summary>
    [Fact]
    public async Task UTCID06_FirstTimeSetPassword_SkipsCurrentPasswordChecks()
    {
        var user = SetupUser(UserWithGoogleOnly());

        var result = await CreateSut().Handle(
            new ChangePasswordCommand(_userId, null, NewPassword), CancellationToken.None);

        Assert.True(result.Success);
        _hasher.Verify(x => x.Verify(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        Assert.Equal($"HASH({NewPassword})", user.PasswordHash);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>UTCID07 · B5=T, B6=T · Type A — đã có mật khẩu nhưng không nhập mật khẩu hiện tại.</summary>
    [Fact]
    public async Task UTCID07_NullCurrentPassword_ReturnsValidationError()
    {
        SetupUser(UserWithPassword());

        var result = await CreateSut().Handle(
            new ChangePasswordCommand(_userId, null, NewPassword), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Mật khẩu hiện tại không được để trống.", result.ErrorMessage);
        _hasher.Verify(x => x.Verify(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    /// <summary>UTCID08 · B6=T · Type B — CurrentPassword rỗng (biên dưới).</summary>
    [Fact]
    public async Task UTCID08_EmptyCurrentPassword_ReturnsValidationError()
    {
        SetupUser(UserWithPassword());

        var result = await CreateSut().Handle(
            new ChangePasswordCommand(_userId, string.Empty, NewPassword), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Mật khẩu hiện tại không được để trống.", result.ErrorMessage);
    }

    /// <summary>UTCID09 · B7=T · Type A — mật khẩu hiện tại sai.</summary>
    [Fact]
    public async Task UTCID09_WrongCurrentPassword_ReturnsWrongPassword()
    {
        var user = SetupUser(UserWithPassword());
        _hasher.Setup(x => x.Verify("WrongPass", user.PasswordHash!)).Returns(false);

        var result = await CreateSut().Handle(
            new ChangePasswordCommand(_userId, "WrongPass", NewPassword), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("WRONG_PASSWORD", result.ErrorCode);
        _hasher.Verify(x => x.HashPassword(It.IsAny<string>()), Times.Never);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>UTCID10 · B8=T · Type A — mật khẩu mới trùng mật khẩu hiện tại.</summary>
    [Fact]
    public async Task UTCID10_NewPasswordSameAsCurrent_ReturnsSamePassword()
    {
        var user = SetupUser(UserWithPassword());
        _hasher.Setup(x => x.Verify(CurrentPassword, user.PasswordHash!)).Returns(true);
        _hasher.Setup(x => x.Verify(NewPassword, user.PasswordHash!)).Returns(true);

        var result = await CreateSut().Handle(
            new ChangePasswordCommand(_userId, CurrentPassword, NewPassword), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("SAME_PASSWORD", result.ErrorCode);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>UTCID11 · B2=F · Type B — mật khẩu mới dài đúng 8 ký tự (biên dưới hợp lệ).</summary>
    [Fact]
    public async Task UTCID11_NewPasswordLengthExactly8_Succeeds()
    {
        var user = SetupUser(UserWithPassword());
        _hasher.Setup(x => x.Verify(CurrentPassword, user.PasswordHash!)).Returns(true);

        var result = await CreateSut().Handle(
            new ChangePasswordCommand(_userId, CurrentPassword, "Pass1234"), CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal("HASH(Pass1234)", user.PasswordHash);
    }

    /// <summary>UTCID12 · Toàn bộ nhánh lỗi = F · Type N — luồng đổi mật khẩu thành công.</summary>
    [Fact]
    public async Task UTCID12_HappyPath_UpdatesPasswordAndSaves()
    {
        var user = SetupUser(UserWithPassword());
        _hasher.Setup(x => x.Verify(CurrentPassword, user.PasswordHash!)).Returns(true);

        var result = await CreateSut().Handle(
            new ChangePasswordCommand(_userId, CurrentPassword, NewPassword), CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal($"HASH({NewPassword})", user.PasswordHash);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
