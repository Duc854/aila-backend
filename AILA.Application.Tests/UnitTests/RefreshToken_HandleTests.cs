using AILA.Application.Common.Interfaces;
using AILA.Application.Common.Interfaces.Repositories;
using AILA.Application.Features.Authentication.Commands.RefreshToken;
using AILA.Application.Tests.UnitTests.TestHelpers;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using Moq;

namespace AILA.Application.Tests.UnitTests;

/// <summary>
/// Sheet: UT14_RefreshToken — <see cref="RefreshTokenCommandHandler.Handle"/>
/// Module: Authentication · CC = 6 · 8 test case
///
/// Nhánh: B1 = RefreshToken rỗng · B2 = không tìm thấy token trong DB
///        B3 = !storedToken.IsValid() (đã thu hồi hoặc hết hạn)
///        B4 = user null · B5 = !user.IsActive
///
/// Bất biến bảo mật (refresh token rotation): token cũ PHẢI bị Revoke và token mới PHẢI được
/// cấp trong cùng một lần lưu — nếu thiếu Revoke thì một refresh token dùng được nhiều lần.
/// Mọi nhánh lỗi đều ném UnauthorizedAccessException, không trả về DTO.
/// </summary>
public class UT14_RefreshToken_HandleTests
{
    private const string RawToken = "RAW-REFRESH-TOKEN";

    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IUserTokenRepository> _userTokens = new();
    private readonly Mock<ITokenProvider> _tokenProvider = new();

    public UT14_RefreshToken_HandleTests()
    {
        _uow.SetupGet(x => x.Users).Returns(_users.Object);
        _uow.SetupGet(x => x.UserTokens).Returns(_userTokens.Object);
        _uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _tokenProvider.Setup(x => x.HashToken(RawToken)).Returns("HASHED-OLD");
        _tokenProvider.Setup(x => x.GenerateAccessToken(It.IsAny<User>())).Returns("NEW-ACCESS");
        _tokenProvider.Setup(x => x.GenerateRefreshToken()).Returns("NEW-REFRESH");
        _tokenProvider.Setup(x => x.HashToken("NEW-REFRESH")).Returns("HASHED-NEW");
    }

    private RefreshTokenCommandHandler CreateSut() => new(_uow.Object, _tokenProvider.Object);

    private static User BuildUser(bool isActive = true)
    {
        var user = new User("learner@aila.vn", "Nguyen Van A", UserRole.Learner, passwordHash: "HASH");
        if (!isActive) user.Deactivate();
        return user;
    }

    private static UserToken BuildToken(Guid userId, bool revoked = false, bool expired = false)
    {
        var token = new UserToken(userId, "HASHED-OLD", DateTime.UtcNow.AddDays(7));
        if (revoked) token.Revoke();
        // ExpiredAt phải > UtcNow ở constructor nên trạng thái "đã hết hạn" chỉ dựng được bằng reflection.
        if (expired) PrivateSetter.Set(token, nameof(UserToken.ExpiredAt), DateTime.UtcNow.AddMinutes(-1));
        return token;
    }

    private void SetupStoredToken(UserToken? token) =>
        _userTokens.Setup(x => x.GetByRefreshTokenHashAsync("HASHED-OLD")).ReturnsAsync(token);

    private Task<Features.Authentication.Dtos.LoginResponseDto> Act(string token = RawToken) =>
        CreateSut().Handle(new RefreshTokenCommand(token), CancellationToken.None);

    /// <summary>UTCID01 · B1=T · Type A — token null.</summary>
    [Fact]
    public async Task UTCID01_NullToken_ThrowsUnauthorized()
    {
        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Act(null!));

        Assert.Equal("Refresh Token không hợp lệ.", ex.Message);
        _userTokens.Verify(x => x.GetByRefreshTokenHashAsync(It.IsAny<string>()), Times.Never);
    }

    /// <summary>UTCID02 · B1=T · Type B — token toàn khoảng trắng.</summary>
    [Fact]
    public async Task UTCID02_WhitespaceToken_ThrowsUnauthorized()
    {
        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Act("   "));

        Assert.Equal("Refresh Token không hợp lệ.", ex.Message);
    }

    /// <summary>UTCID03 · B2=T · Type A — token không tồn tại trong DB.</summary>
    [Fact]
    public async Task UTCID03_TokenNotFound_ThrowsUnauthorized()
    {
        SetupStoredToken(null);

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Act());

        Assert.Equal("Refresh Token không tồn tại.", ex.Message);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>UTCID04 · B3=T · Type A — token đã bị thu hồi (đăng xuất trước đó).</summary>
    [Fact]
    public async Task UTCID04_RevokedToken_ThrowsUnauthorized()
    {
        SetupStoredToken(BuildToken(Guid.NewGuid(), revoked: true));

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Act());

        Assert.Equal("Refresh Token đã hết hạn hoặc bị thu hồi.", ex.Message);
        _userTokens.Verify(x => x.Add(It.IsAny<UserToken>()), Times.Never);
    }

    /// <summary>UTCID05 · B3=T · Type B — token quá hạn (biên: ExpiredAt lùi 1 phút).</summary>
    [Fact]
    public async Task UTCID05_ExpiredToken_ThrowsUnauthorized()
    {
        SetupStoredToken(BuildToken(Guid.NewGuid(), expired: true));

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Act());

        Assert.Equal("Refresh Token đã hết hạn hoặc bị thu hồi.", ex.Message);
    }

    /// <summary>UTCID06 · B4=T · Type A — token hợp lệ nhưng user đã bị xoá.</summary>
    [Fact]
    public async Task UTCID06_UserNotFound_ThrowsUnauthorized()
    {
        var userId = Guid.NewGuid();
        SetupStoredToken(BuildToken(userId));
        _users.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync((User?)null);

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Act());

        Assert.Equal("User không hợp lệ.", ex.Message);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>UTCID07 · B4=F, B5=T · Type A — tài khoản đã bị khóa giữa chừng.</summary>
    [Fact]
    public async Task UTCID07_InactiveUser_ThrowsUnauthorized()
    {
        var user = BuildUser(isActive: false);
        var storedToken = BuildToken(user.Id);
        SetupStoredToken(storedToken);
        _users.Setup(x => x.GetByIdAsync(user.Id)).ReturnsAsync(user);

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Act());

        Assert.Equal("User không hợp lệ.", ex.Message);
        Assert.False(storedToken.IsRevoked);
        _userTokens.Verify(x => x.Add(It.IsAny<UserToken>()), Times.Never);
    }

    /// <summary>
    /// UTCID08 · Toàn bộ nhánh = F · Type N — xoay vòng token thành công.
    /// Bắt buộc: token CŨ bị Revoke, token MỚI được Add, lưu đúng 1 lần.
    /// </summary>
    [Fact]
    public async Task UTCID08_HappyPath_RevokesOldTokenAndIssuesNewOne()
    {
        var user = BuildUser();
        var storedToken = BuildToken(user.Id);
        SetupStoredToken(storedToken);
        _users.Setup(x => x.GetByIdAsync(user.Id)).ReturnsAsync(user);

        var result = await Act();

        Assert.Equal("NEW-ACCESS", result.AccessToken);
        Assert.Equal("NEW-REFRESH", result.RefreshToken);
        Assert.Equal(user.Id, result.UserId);
        Assert.Equal(nameof(UserRole.Learner), result.Role);

        Assert.True(storedToken.IsRevoked);
        _userTokens.Verify(x => x.Add(It.Is<UserToken>(t =>
            t.UserId == user.Id && t.RefreshTokenHash == "HASHED-NEW")), Times.Once);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
