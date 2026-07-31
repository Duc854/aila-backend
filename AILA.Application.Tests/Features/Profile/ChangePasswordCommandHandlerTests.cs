using AILA.Application.Common.Interfaces;
using AILA.Application.Common.Interfaces.Repositories;
using AILA.Application.Features.Profile.Commands.ChangePassword;
using AILA.Application.Tests.Common.Builders;
using AILA.Domain.Entities;
using Moq;

namespace AILA.Application.Tests.Features.Profile
{
    /// <summary>
    /// Sheet: AuthService · Method Under Test: changePassword(ChangePasswordRequest) · UC-11.
    /// TC-UNIT-AuthService-018 → 021.
    ///
    /// Handler này lấy repository qua <c>uow.Repository&lt;User&gt;()</c> (generic) chứ không
    /// qua property <c>uow.Users</c> — setup mock phải khớp đúng cách gọi đó.
    /// </summary>
    public class ChangePasswordCommandHandlerTests
    {
        private readonly Mock<IGenericRepository<User>> _users = new();
        private readonly Mock<IPasswordHasher> _hasher = new();
        private readonly Mock<IUnitOfWork> _uow = new();
        private readonly ChangePasswordCommandHandler _handler;

        public ChangePasswordCommandHandlerTests()
        {
            _uow.Setup(u => u.Repository<User>()).Returns(_users.Object);
            _handler = new ChangePasswordCommandHandler(_uow.Object, _hasher.Object);
        }

        // ------------------------------------------------------------ TC-018
        // Covers: Main Flow — đổi thành công thì lưu HASH mới, đúng một lần lưu.
        [Fact]
        [Trait("TC", "TC-UNIT-AuthService-018")]
        [Trait("UC", "UC-11")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task Handle_ValidPasswords_UpdatesHash()
        {
            var user = new UserBuilder().WithPasswordHash("$old").Build();
            _users.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);
            _hasher.Setup(h => h.Verify("OldPass1", "$old")).Returns(true);
            _hasher.Setup(h => h.Verify("NewPass1", "$old")).Returns(false);
            _hasher.Setup(h => h.HashPassword("NewPass1")).Returns("$new");

            var result = await _handler.Handle(
                new ChangePasswordCommand(user.Id, "OldPass1", "NewPass1"), CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal("$new", user.PasswordHash);
            Assert.NotNull(user.UpdatedAt);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        // ------------------------------------------------------------ TC-019
        // Covers: BR-01 current match — sai mật khẩu hiện tại thì không băm, không lưu.
        [Fact]
        [Trait("TC", "TC-UNIT-AuthService-019")]
        [Trait("UC", "UC-11")]
        [Trait("BR", "BR-01")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task Handle_WrongCurrent_RejectedNoSave()
        {
            var user = new UserBuilder().WithPasswordHash("$old").Build();
            _users.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);
            _hasher.Setup(h => h.Verify("wrong", "$old")).Returns(false);

            var result = await _handler.Handle(
                new ChangePasswordCommand(user.Id, "wrong", "NewPass1"), CancellationToken.None);

            Assert.False(result.Success);
            Assert.Equal("WRONG_PASSWORD", result.ErrorCode);
            Assert.Equal("$old", user.PasswordHash);
            _hasher.Verify(h => h.HashPassword(It.IsAny<string>()), Times.Never);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        // ------------------------------------------------------------ TC-020
        // Covers: AF-01 invalid password. Policy của ChangePassword hiện là ĐỘ DÀI ≥ 8, kiểm
        // NGAY đầu handler nên chưa hề chạm tới DB.
        //
        // ⚠ Lệch chuẩn còn lại: ChangePassword và ResetPassword đều dùng ngưỡng ≥8, nhưng
        // Register KHÔNG kiểm policy nào — xem DEF-AUTH-02.
        [Theory]
        [InlineData("")]           // rỗng
        [InlineData("   ")]        // chỉ khoảng trắng
        [InlineData("Short1")]     // 6 ký tự
        [InlineData("Short1A")]    // 7 ký tự — biên - 1
        [Trait("TC", "TC-UNIT-AuthService-020")]
        [Trait("UC", "UC-11")]
        [Trait("Type", "Input Validation")]
        [Trait("Priority", "High")]
        public async Task Handle_WeakNewPassword_RejectedNoDb(string newPassword)
        {
            var result = await _handler.Handle(
                new ChangePasswordCommand(Guid.NewGuid(), "OldPass1", newPassword), CancellationToken.None);

            Assert.False(result.Success);
            Assert.Equal("VALIDATION_ERROR", result.ErrorCode);
            _users.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        // ------------------------------------------------------------ TC-020b
        // Biên dưới của policy: đúng 8 ký tự và đủ 3 loại ký tự thì phải ĐƯỢC chấp nhận.
        [Fact]
        [Trait("TC", "TC-UNIT-AuthService-021")]
        [Trait("UC", "UC-11")]
        [Trait("Type", "Boundary & Negative")]
        [Trait("Priority", "Medium")]
        public async Task Handle_NewPasswordAtMinLength_Ok()
        {
            var user = new UserBuilder().WithPasswordHash("$old").Build();
            _users.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);
            _hasher.Setup(h => h.Verify("OldPass1", "$old")).Returns(true);
            _hasher.Setup(h => h.Verify("Abcdef12", "$old")).Returns(false);
            _hasher.Setup(h => h.HashPassword("Abcdef12")).Returns("$new");

            var result = await _handler.Handle(
                new ChangePasswordCommand(user.Id, "OldPass1", "Abcdef12"), CancellationToken.None);

            Assert.True(result.Success);
        }

        // ------------------------------------------------------------ TC-021
        // Covers: BR-03 must differ — mật khẩu mới trùng mật khẩu cũ thì từ chối, không lưu.
        [Fact]
        [Trait("TC", "TC-UNIT-AuthService-029")]
        [Trait("UC", "UC-11")]
        [Trait("BR", "BR-03")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        public async Task Handle_SameAsCurrent_RejectedNoSave()
        {
            var user = new UserBuilder().WithPasswordHash("$old").Build();
            _users.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);
            // Cùng một chuỗi dùng cho cả current và new ⇒ Verify trả true ở cả hai lần gọi.
            _hasher.Setup(h => h.Verify("OldPass1", "$old")).Returns(true);

            var result = await _handler.Handle(
                new ChangePasswordCommand(user.Id, "OldPass1", "OldPass1"), CancellationToken.None);

            Assert.False(result.Success);
            Assert.Equal("SAME_PASSWORD", result.ErrorCode);
            Assert.Equal("$old", user.PasswordHash);
            _hasher.Verify(h => h.HashPassword(It.IsAny<string>()), Times.Never);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        // ------------------------------------------------------------ TC-021b
        // Nhánh chưa TC nào phủ: tài khoản bị khoá không được đổi mật khẩu.
        // → cần thêm dòng TC-UNIT-AuthService-029 vào sheet AuthService.
        [Fact]
        [Trait("TC", "TC-UNIT-AuthService-029")]
        [Trait("UC", "UC-11")]
        [Trait("Type", "Security (GBR)")]
        [Trait("Priority", "High")]
        public async Task Handle_InactiveAccount_ReturnsAccountInactive()
        {
            var user = new UserBuilder().WithPasswordHash("$old").Inactive().Build();
            _users.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);

            var result = await _handler.Handle(
                new ChangePasswordCommand(user.Id, "OldPass1", "NewPass1"), CancellationToken.None);

            Assert.False(result.Success);
            Assert.Equal("ACCOUNT_INACTIVE", result.ErrorCode);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
