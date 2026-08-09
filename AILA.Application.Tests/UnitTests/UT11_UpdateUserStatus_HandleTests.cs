using AILA.Application.Common.Interfaces;
using AILA.Application.Common.Interfaces.Repositories;
using AILA.Application.Features.Users.Commands.UpdateUserStatus;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using Moq;

namespace AILA.Application.Tests.UnitTests;

/// <summary>
/// Sheet: UT11_UpdateUserStatus — <see cref="UpdateUserStatusCommandHandler.Handle"/>
/// Module: UserManagement · CC = 5 · 7 test case
///
/// Nhánh: B1 = UserId rỗng · B2 = user null · B3 = user là Admin (HÀNG RÀO PHÂN QUYỀN)
///        B4 = request.IsActive (Activate / Deactivate)
///
/// B3 là nhánh rủi ro cao nhất: phải Verify SaveChangesAsync = Never, nếu chỉ assert
/// ErrorCode thì lỗi "đã Deactivate rồi mới trả lỗi" vẫn lọt.
/// </summary>
public class UT11_UpdateUserStatus_HandleTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Guid _userId = Guid.NewGuid();

    public UT11_UpdateUserStatus_HandleTests()
    {
        _uow.SetupGet(x => x.Users).Returns(_users.Object);
        _uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    private UpdateUserStatusCommandHandler CreateSut() => new(_uow.Object);

    private User SetupUser(UserRole role, bool isActive)
    {
        var user = new User("user@aila.vn", "Nguyen Van A", role, passwordHash: "HASH");
        if (!isActive) user.Deactivate();

        _users.Setup(x => x.GetByIdAsync(_userId)).ReturnsAsync(user);
        return user;
    }

    private Task<Shared.Wrappers.ResponseDto<Features.Users.Dtos.UserDetailDto>> Act(bool isActive, Guid? userId = null) =>
        CreateSut().Handle(new UpdateUserStatusCommand(userId ?? _userId, isActive), CancellationToken.None);

    /// <summary>UTCID01 · B1=T · Type A — UserId rỗng, chặn trước khi chạm repository.</summary>
    [Fact]
    public async Task UTCID01_EmptyUserId_ReturnsInvalidUserId()
    {
        var result = await Act(true, Guid.Empty);

        Assert.False(result.Success);
        Assert.Equal("INVALID_USER_ID", result.ErrorCode);
        _users.Verify(x => x.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>UTCID02 · B2=T · Type A — không tìm thấy tài khoản.</summary>
    [Fact]
    public async Task UTCID02_UserNotFound_ReturnsUserNotFound()
    {
        _users.Setup(x => x.GetByIdAsync(_userId)).ReturnsAsync((User?)null);

        var result = await Act(true);

        Assert.False(result.Success);
        Assert.Equal("USER_NOT_FOUND", result.ErrorCode);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// UTCID03 · B3=T · Type A — không được đổi trạng thái tài khoản Admin.
    /// Bắt buộc kiểm tra trạng thái user KHÔNG bị thay đổi và KHÔNG lưu DB.
    /// </summary>
    [Fact]
    public async Task UTCID03_AdminAccount_ReturnsAccessDeniedWithoutChangingState()
    {
        var admin = SetupUser(UserRole.Admin, isActive: true);

        var result = await Act(false);

        Assert.False(result.Success);
        Assert.Equal("ACCESS_DENIED", result.ErrorCode);
        Assert.Equal("Không thể cập nhật trạng thái tài khoản Admin.", result.ErrorMessage);
        Assert.True(admin.IsActive);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>UTCID04 · B3=F, B4=F · Type N — vô hiệu hoá tài khoản Learner đang hoạt động.</summary>
    [Fact]
    public async Task UTCID04_DeactivateActiveLearner_Succeeds()
    {
        var user = SetupUser(UserRole.Learner, isActive: true);

        var result = await Act(false);

        Assert.True(result.Success);
        Assert.False(result.Data!.IsActive);
        Assert.False(user.IsActive);
        Assert.Equal(user.Id, result.Data.Id);
        Assert.Equal(user.Email, result.Data.Email);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>UTCID05 · B4=T · Type N — kích hoạt lại tài khoản Learner đang bị khoá.</summary>
    [Fact]
    public async Task UTCID05_ActivateInactiveLearner_Succeeds()
    {
        var user = SetupUser(UserRole.Learner, isActive: false);

        var result = await Act(true);

        Assert.True(result.Success);
        Assert.True(result.Data!.IsActive);
        Assert.True(user.IsActive);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>UTCID06 · B4=T · Type N — idempotent: kích hoạt tài khoản Expert vốn đang hoạt động.</summary>
    [Fact]
    public async Task UTCID06_ActivateAlreadyActiveExpert_IsIdempotent()
    {
        var user = SetupUser(UserRole.Expert, isActive: true);

        var result = await Act(true);

        Assert.True(result.Success);
        Assert.True(result.Data!.IsActive);
        Assert.True(user.IsActive);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>UTCID07 · B4=F · Type B — idempotent: vô hiệu hoá tài khoản Expert vốn đã bị khoá.</summary>
    [Fact]
    public async Task UTCID07_DeactivateAlreadyInactiveExpert_IsIdempotent()
    {
        var user = SetupUser(UserRole.Expert, isActive: false);

        var result = await Act(false);

        Assert.True(result.Success);
        Assert.False(result.Data!.IsActive);
        Assert.False(user.IsActive);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
