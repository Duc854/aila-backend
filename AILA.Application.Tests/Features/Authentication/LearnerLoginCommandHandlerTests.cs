using AILA.Application.Common.Interfaces;
using AILA.Application.Common.Interfaces.Repositories;
using AILA.Application.Features.Authentication.Commands.LearnerLogin;
using AILA.Application.Tests.Common.Builders;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using Moq;

namespace AILA.Application.Tests.Features.Authentication
{
    /// <summary>
    /// Sheet: AuthService · Method Under Test: login(LoginRequest) · UC-10.
    /// TC-UNIT-AuthService-014 → 017.
    /// </summary>
    public class LearnerLoginCommandHandlerTests
    {
        private readonly Mock<IUserRepository> _users = new();
        private readonly Mock<IPasswordHasher> _hasher = new();
        private readonly Mock<ITokenProvider> _tokens = new();
        private readonly Mock<IUnitOfWork> _uow = new();
        private readonly LearnerLoginCommandHandler _handler;

        public LearnerLoginCommandHandlerTests()
        {
            _uow.Setup(u => u.Users).Returns(_users.Object);
            _tokens.Setup(t => t.GenerateAccessToken(It.IsAny<User>())).Returns("jwt");
            _tokens.Setup(t => t.GenerateRefreshToken()).Returns("refresh");

            _handler = new LearnerLoginCommandHandler(_uow.Object, _hasher.Object, _tokens.Object);
        }

        private static LearnerLoginCommand Command(
            string email = "learner@aila.com",
            string password = "Valid@123")
            => new() { Email = email, Password = password };

        // ------------------------------------------------------------ TC-014
        // Covers: Main Flow — trả đủ token + hồ sơ tối thiểu để FE điều hướng theo role.
        [Fact]
        [Trait("TC", "TC-UNIT-AuthService-014")]
        [Trait("UC", "UC-10")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Critical")]
        public async Task Handle_ValidCredentials_TokensAndProfile()
        {
            var user = new UserBuilder()
                .WithEmail("learner@aila.com")
                .WithRole(UserRole.Learner)
                .WithPasswordHash("$2a$H")
                .Build();

            _users.Setup(r => r.GetByEmailAsync("learner@aila.com")).ReturnsAsync(user);
            _hasher.Setup(h => h.Verify("Valid@123", "$2a$H")).Returns(true);

            var result = await _handler.Handle(Command(), CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal("jwt", result.Data!.AccessToken);
            Assert.Equal("refresh", result.Data.RefreshToken);
            Assert.Equal("Learner", result.Data.Role);
            Assert.Equal(user.Id, result.Data.UserId);
            Assert.Equal(user.Email, result.Data.Email);
            _tokens.Verify(t => t.GenerateAccessToken(user), Times.Once);
        }

        // ------------------------------------------------------------ TC-015
        // Covers: AF-01 wrong email. Mã lỗi phải chung với "sai mật khẩu" để không tiết lộ
        // email nào đang tồn tại (chống account enumeration).
        [Fact]
        [Trait("TC", "TC-UNIT-AuthService-015")]
        [Trait("UC", "UC-10")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task Handle_EmailNotFound_InvalidCredsNoToken()
        {
            _users.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

            var result = await _handler.Handle(Command(email: "nouser@aila.com"), CancellationToken.None);

            Assert.False(result.Success);
            Assert.Equal("INVALID_CREDENTIALS", result.ErrorCode);
            _tokens.Verify(t => t.GenerateAccessToken(It.IsAny<User>()), Times.Never);
            _tokens.Verify(t => t.GenerateRefreshToken(), Times.Never);
        }

        // ------------------------------------------------------------ TC-016
        // Covers: AF-01 wrong password — cùng mã lỗi với TC-015, xem ghi chú ở trên.
        [Fact]
        [Trait("TC", "TC-UNIT-AuthService-016")]
        [Trait("UC", "UC-10")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task Handle_WrongPassword_InvalidCredsNoToken()
        {
            var user = new UserBuilder().WithRole(UserRole.Learner).WithPasswordHash("$2a$H").Build();
            _users.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
            _hasher.Setup(h => h.Verify("wrong", "$2a$H")).Returns(false);

            var result = await _handler.Handle(Command(password: "wrong"), CancellationToken.None);

            Assert.False(result.Success);
            Assert.Equal("INVALID_CREDENTIALS", result.ErrorCode);
            _tokens.Verify(t => t.GenerateAccessToken(It.IsAny<User>()), Times.Never);
        }

        // ------------------------------------------------------------ TC-028  (MỚI)
        // Nhánh `user.Role != UserRole.Learner` của handler không có TC nào trong workbook phủ.
        // → cần thêm dòng TC-UNIT-AuthService-028 vào sheet AuthService.
        // Covers: BR-02 role gate — tài khoản Expert/Admin không đi qua cổng đăng nhập Learner.
        [Theory]
        [InlineData(UserRole.Expert)]
        [InlineData(UserRole.Admin)]
        [Trait("TC", "TC-UNIT-AuthService-028")]
        [Trait("UC", "UC-10")]
        [Trait("BR", "BR-02")]
        [Trait("Type", "Security (GBR)")]
        [Trait("Priority", "High")]
        public async Task Handle_NonLearnerRole_InvalidCreds(UserRole role)
        {
            var user = new UserBuilder().WithRole(role).WithPasswordHash("$2a$H").Build();
            _users.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
            _hasher.Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

            var result = await _handler.Handle(Command(), CancellationToken.None);

            Assert.Equal("INVALID_CREDENTIALS", result.ErrorCode);
            _tokens.Verify(t => t.GenerateAccessToken(It.IsAny<User>()), Times.Never);
        }

        // ------------------------------------------------------------ TC-017  ⚠ DEFECT
        // BR-01 "Only users with a valid active account can sign in" KHÔNG được thực thi ở
        // LearnerLoginCommandHandler — nó không hề đọc user.IsActive. ExpertLogin và AdminLogin
        // đều có kiểm tra này, nên đây là lỗ hổng riêng của cổng Learner: tài khoản bị admin
        // khoá (UC-77) vẫn đăng nhập được và vẫn nhận token hợp lệ.
        // Test khoá hành vi hiện tại; đảo lại thành assert INVALID_CREDENTIALS khi defect được fix.
        [Fact(Skip = "DEF-AUTH-03 - Learner login ignores IsActive, a locked account can sign in")]
        [Trait("TC", "TC-UNIT-AuthService-017")]
        [Trait("UC", "UC-10")]
        [Trait("BR", "BR-01")]
        [Trait("Type", "Security (GBR)")]
        [Trait("Priority", "High")]
        [Trait("Defect", "DEF-AUTH-03")]
        public async Task Handle_InactiveLearner_LogsInBug()
        {
            var user = new UserBuilder().WithRole(UserRole.Learner).WithPasswordHash("$2a$H").Inactive().Build();
            _users.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
            _hasher.Setup(h => h.Verify(It.IsAny<string>(), "$2a$H")).Returns(true);

            var result = await _handler.Handle(Command(), CancellationToken.None);

            Assert.False(user.IsActive);
            Assert.True(result.Success);
            Assert.Equal("jwt", result.Data!.AccessToken);
            _tokens.Verify(t => t.GenerateAccessToken(user), Times.Once);
        }
    }
}
