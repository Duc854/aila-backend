using AILA.Application.Common.Interfaces;
using AILA.Application.Common.Interfaces.Repositories;
using AILA.Application.Features.Authentication.Commands.LearnerLogin;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using Moq;

namespace AILA.Application.Tests.UnitTests;

/// <summary>
/// Sheet: UT13_LearnerLogin — <see cref="LearnerLoginCommandHandler.Handle"/>
/// Module: Authentication · CC = 5 · 7 test case
///
/// Nhánh: B1 = user null · B2 = Role != Learner · B3 = !IsActive · B4 = sai mật khẩu
///
/// Bất biến bảo mật: B1 và B2 trả về CÙNG một mã lỗi INVALID_CREDENTIALS với B4 —
/// không tiết lộ email có tồn tại hay không, cũng không tiết lộ sai vai trò.
/// </summary>
public class UT13_LearnerLogin_HandleTests
{
    private const string Email = "learner@aila.vn";
    private const string Password = "Password123!";

    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IUserTokenRepository> _userTokens = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<ITokenProvider> _tokenProvider = new();

    public UT13_LearnerLogin_HandleTests()
    {
        _uow.SetupGet(x => x.Users).Returns(_users.Object);
        _uow.SetupGet(x => x.UserTokens).Returns(_userTokens.Object);
        _uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _tokenProvider.Setup(x => x.GenerateAccessToken(It.IsAny<User>())).Returns("ACCESS-TOKEN");
        _tokenProvider.Setup(x => x.GenerateRefreshToken()).Returns("REFRESH-TOKEN");
        _tokenProvider.Setup(x => x.HashToken("REFRESH-TOKEN")).Returns("HASHED-REFRESH");
        _hasher.Setup(x => x.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(false);
    }

    private LearnerLoginCommandHandler CreateSut() =>
        new(_uow.Object, _hasher.Object, _tokenProvider.Object);

    private static User BuildUser(UserRole role = UserRole.Learner, bool isActive = true,
        string? passwordHash = "OLD_HASH")
    {
        var user = passwordHash is null
            ? new User(Email, "Nguyen Van A", role, googleId: "google-sub-123")
            : new User(Email, "Nguyen Van A", role, passwordHash: passwordHash);

        if (!isActive) user.Deactivate();
        return user;
    }

    private User SetupUser(User? user)
    {
        _users.Setup(x => x.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
        return user!;
    }

    private Task<Shared.Wrappers.ResponseDto<Features.Authentication.Dtos.LoginResponseDto>> Act() =>
        CreateSut().Handle(new LearnerLoginCommand { Email = Email, Password = Password }, CancellationToken.None);

    /// <summary>UTCID01 · B1=T · Type A — email không tồn tại.</summary>
    [Fact]
    public async Task UTCID01_UserNotFound_ReturnsInvalidCredentials()
    {
        SetupUser(null);

        var result = await Act();

        Assert.False(result.Success);
        Assert.Equal("INVALID_CREDENTIALS", result.ErrorCode);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// UTCID02 · B1=F, B2=T · Type A — tài khoản Expert đăng nhập nhầm cổng Learner.
    /// Trả cùng mã lỗi với "sai mật khẩu" để không tiết lộ vai trò tài khoản.
    /// </summary>
    [Fact]
    public async Task UTCID02_ExpertAccount_ReturnsInvalidCredentials()
    {
        SetupUser(BuildUser(role: UserRole.Expert));

        var result = await Act();

        Assert.False(result.Success);
        Assert.Equal("INVALID_CREDENTIALS", result.ErrorCode);
        _hasher.Verify(x => x.Verify(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    /// <summary>UTCID03 · B2=T · Type B — tài khoản Admin (biên trên của enum UserRole).</summary>
    [Fact]
    public async Task UTCID03_AdminAccount_ReturnsInvalidCredentials()
    {
        SetupUser(BuildUser(role: UserRole.Admin));

        var result = await Act();

        Assert.False(result.Success);
        Assert.Equal("INVALID_CREDENTIALS", result.ErrorCode);
    }

    /// <summary>UTCID04 · B3=T · Type A — tài khoản bị khóa.</summary>
    [Fact]
    public async Task UTCID04_InactiveAccount_ReturnsAccountBanned()
    {
        SetupUser(BuildUser(isActive: false));

        var result = await Act();

        Assert.False(result.Success);
        Assert.Equal("ACCOUNT_BANNED", result.ErrorCode);
        Assert.Equal("Tài khoản của bạn đã bị khóa.", result.ErrorMessage);
        _hasher.Verify(x => x.Verify(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    /// <summary>UTCID05 · B4=T · Type A — sai mật khẩu.</summary>
    [Fact]
    public async Task UTCID05_WrongPassword_ReturnsInvalidCredentials()
    {
        var user = SetupUser(BuildUser());
        _hasher.Setup(x => x.Verify(Password, user.PasswordHash!)).Returns(false);

        var result = await Act();

        Assert.False(result.Success);
        Assert.Equal("INVALID_CREDENTIALS", result.ErrorCode);
        _userTokens.Verify(x => x.Add(It.IsAny<UserToken>()), Times.Never);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>UTCID06 · Toàn bộ nhánh = F · Type N — đăng nhập thành công.</summary>
    [Fact]
    public async Task UTCID06_HappyPath_IssuesTokensAndPersistsRefreshToken()
    {
        var user = SetupUser(BuildUser());
        _hasher.Setup(x => x.Verify(Password, user.PasswordHash!)).Returns(true);

        var result = await Act();

        Assert.True(result.Success);
        Assert.Equal("ACCESS-TOKEN", result.Data!.AccessToken);
        Assert.Equal("REFRESH-TOKEN", result.Data.RefreshToken);
        Assert.Equal(nameof(UserRole.Learner), result.Data.Role);
        Assert.Equal(user.Id, result.Data.UserId);
        Assert.Equal(user.Email, result.Data.Email);

        _userTokens.Verify(x => x.Add(It.Is<UserToken>(t =>
            t.UserId == user.Id && t.RefreshTokenHash == "HASHED-REFRESH")), Times.Once);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// UTCID07 · B4 với PasswordHash = null · Type B — tài khoản đăng nhập bằng Google.
    ///
    /// DEFECT DFID004: handler gọi thẳng _passwordHasher.Verify(password, user.PasswordHash!)
    /// mà KHÔNG kiểm tra PasswordHash có null hay không. Cài đặt thật là
    /// BcryptPasswordHasher → BCrypt.Net.BCrypt.Verify(text, null) sẽ ném exception
    /// ⇒ tài khoản Google đăng nhập nhầm cổng mật khẩu nhận HTTP 500 thay vì 401.
    ///
    /// Expected ghi theo hành vi ĐÚNG: trả INVALID_CREDENTIALS và KHÔNG gọi Verify với hash null.
    /// ⇒ Test này DỰ KIẾN FAIL cho tới khi bổ sung guard.
    /// </summary>
    [Fact]
    public async Task UTCID07_GoogleOnlyAccount_ShouldNotCallVerifyWithNullHash()
    {
        SetupUser(BuildUser(passwordHash: null));

        var result = await Act();

        Assert.False(result.Success);
        Assert.Equal("INVALID_CREDENTIALS", result.ErrorCode);
        _hasher.Verify(x => x.Verify(It.IsAny<string>(), null!), Times.Never);
    }
}
