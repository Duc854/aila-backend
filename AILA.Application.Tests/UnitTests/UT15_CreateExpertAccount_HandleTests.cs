using AILA.Application.Common.Interfaces;
using AILA.Application.Common.Interfaces.Repositories;
using AILA.Application.Features.Users.Commands.CreateExpertAccount;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using Moq;

namespace AILA.Application.Tests.UnitTests;

/// <summary>
/// Sheet: UT15_CreateExpertAccount — <see cref="CreateExpertAccountCommandHandler.Handle"/>
/// Module: UserManagement · CC = 8 · 10 test case
///
/// Nhánh: B1 = FullName rỗng · B2 = Email rỗng · B3 = Email sai định dạng
///        B4 = Password rỗng · B5 = Password.Length &lt; 6 · B6 = email đã tồn tại
///        B7 = catch Exception
///
/// Lưu ý policy: mật khẩu ở đây yêu cầu tối thiểu 6 ký tự — KHÁC với PasswordPolicy (8 ký tự)
/// dùng ở luồng Reset Password (UT02) và ChangePassword (UT04). Bộ test khoá lại sự khác biệt này.
/// </summary>
public class UT15_CreateExpertAccount_HandleTests
{
    private const string FullName = "Nguyen Van Chuyen Gia";
    private const string Email = "expert@aila.vn";
    private const string Password = "Expert123";

    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IPasswordHasher> _hasher = new();

    public UT15_CreateExpertAccount_HandleTests()
    {
        _uow.SetupGet(x => x.Users).Returns(_users.Object);
        _hasher.Setup(x => x.HashPassword(It.IsAny<string>())).Returns<string>(p => $"HASH({p})");
        _users.Setup(x => x.EmailExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(false);
        _users.Setup(x => x.CreateUserWithExpertProfileAsync(
                  It.IsAny<User>(), It.IsAny<Expert>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync((User u, Expert _, CancellationToken _) => u);
    }

    private CreateExpertAccountCommandHandler CreateSut() => new(_uow.Object, _hasher.Object);

    private Task<Shared.Wrappers.ResponseDto<Features.Users.Dtos.UserDetailDto>> Act(
        string fullName = FullName, string email = Email, string password = Password) =>
        CreateSut().Handle(new CreateExpertAccountCommand(fullName, email, password), CancellationToken.None);

    private void AssertNothingCreated() =>
        _users.Verify(x => x.CreateUserWithExpertProfileAsync(
            It.IsAny<User>(), It.IsAny<Expert>(), It.IsAny<CancellationToken>()), Times.Never);

    /// <summary>UTCID01 · B1=T · Type A — họ tên null.</summary>
    [Fact]
    public async Task UTCID01_NullFullName_ReturnsInvalidFullName()
    {
        var result = await Act(fullName: null!);

        Assert.False(result.Success);
        Assert.Equal("INVALID_FULL_NAME", result.ErrorCode);
        AssertNothingCreated();
    }

    /// <summary>UTCID02 · B1=T · Type B — họ tên toàn khoảng trắng.</summary>
    [Fact]
    public async Task UTCID02_WhitespaceFullName_ReturnsInvalidFullName()
    {
        var result = await Act(fullName: "   ");

        Assert.False(result.Success);
        Assert.Equal("INVALID_FULL_NAME", result.ErrorCode);
    }

    /// <summary>UTCID03 · B1=F, B2=T · Type A — email null.</summary>
    [Fact]
    public async Task UTCID03_NullEmail_ReturnsInvalidEmail()
    {
        var result = await Act(email: null!);

        Assert.False(result.Success);
        Assert.Equal("INVALID_EMAIL", result.ErrorCode);
        AssertNothingCreated();
    }

    /// <summary>UTCID04 · B2=F, B3=T · Type A — email sai định dạng.</summary>
    [Fact]
    public async Task UTCID04_MalformedEmail_ReturnsInvalidEmail()
    {
        var result = await Act(email: "expertaila.vn");

        Assert.False(result.Success);
        Assert.Equal("INVALID_EMAIL", result.ErrorCode);
    }

    /// <summary>UTCID05 · B4=T · Type A — mật khẩu null.</summary>
    [Fact]
    public async Task UTCID05_NullPassword_ReturnsInvalidPassword()
    {
        var result = await Act(password: null!);

        Assert.False(result.Success);
        Assert.Equal("INVALID_PASSWORD", result.ErrorCode);
        Assert.Equal("Mật khẩu phải có ít nhất 6 ký tự.", result.ErrorMessage);
    }

    /// <summary>UTCID06 · B4=F, B5=T · Type B — mật khẩu 5 ký tự (biên dưới không hợp lệ).</summary>
    [Fact]
    public async Task UTCID06_PasswordLength5_ReturnsInvalidPassword()
    {
        var result = await Act(password: "12345");

        Assert.False(result.Success);
        Assert.Equal("INVALID_PASSWORD", result.ErrorCode);
        AssertNothingCreated();
    }

    /// <summary>
    /// UTCID07 · B5=F · Type B — mật khẩu đúng 6 ký tự (biên dưới hợp lệ).
    /// Khoá lại khác biệt policy: 6 ký tự bị PasswordPolicy (UT02) từ chối nhưng ở đây hợp lệ.
    /// </summary>
    [Fact]
    public async Task UTCID07_PasswordLengthExactly6_Succeeds()
    {
        var result = await Act(password: "123456");

        Assert.True(result.Success);
        _hasher.Verify(x => x.HashPassword("123456"), Times.Once);
    }

    /// <summary>UTCID08 · B6=T · Type A — email đã tồn tại trong hệ thống.</summary>
    [Fact]
    public async Task UTCID08_DuplicateEmail_ReturnsDuplicateEmail()
    {
        _users.Setup(x => x.EmailExistsAsync(Email, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await Act();

        Assert.False(result.Success);
        Assert.Equal("DUPLICATE_EMAIL", result.ErrorCode);
        AssertNothingCreated();
    }

    /// <summary>
    /// UTCID09 · Toàn bộ nhánh = F · Type N — tạo tài khoản Expert thành công.
    /// Email được chuẩn hoá (trim + lowercase) trước khi kiểm tra trùng và lưu.
    /// </summary>
    [Fact]
    public async Task UTCID09_HappyPath_CreatesExpertUserWithNormalizedEmail()
    {
        var result = await Act(fullName: "  Nguyen Van Chuyen Gia  ", email: "  EXPERT@AILA.VN  ");

        Assert.True(result.Success);
        Assert.Equal(Email, result.Data!.Email);
        Assert.Equal("Nguyen Van Chuyen Gia", result.Data.FullName);
        Assert.Equal(UserRole.Expert, result.Data.Role);
        Assert.True(result.Data.IsActive);

        _users.Verify(x => x.EmailExistsAsync(Email, It.IsAny<CancellationToken>()), Times.Once);
        _users.Verify(x => x.CreateUserWithExpertProfileAsync(
            It.Is<User>(u => u.Email == Email && u.Role == UserRole.Expert),
            It.Is<Expert>(e => e.YearsOfExperience == 0),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>UTCID10 · B7=T · Type A — repository ném exception ⇒ bọc thành mã lỗi nghiệp vụ.</summary>
    [Fact]
    public async Task UTCID10_RepositoryThrows_ReturnsCreateExpertError()
    {
        _users.Setup(x => x.CreateUserWithExpertProfileAsync(
                  It.IsAny<User>(), It.IsAny<Expert>(), It.IsAny<CancellationToken>()))
              .ThrowsAsync(new InvalidOperationException("db down"));

        var result = await Act();

        Assert.False(result.Success);
        Assert.Equal("CREATE_EXPERT_ERROR", result.ErrorCode);
        Assert.Contains("db down", result.ErrorMessage);
    }
}
