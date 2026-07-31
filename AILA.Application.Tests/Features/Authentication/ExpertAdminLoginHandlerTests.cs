using AILA.Application.Common.Interfaces;
using AILA.Application.Common.Interfaces.Repositories;
using AILA.Application.Features.Authentication.Commands.AdminLogin;
using AILA.Application.Features.Authentication.Commands.ExpertLogin;
using AILA.Application.Tests.Common.Builders;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using Moq;

namespace AILA.Application.Tests.Features.Authentication
{
    /// <summary>
    /// Sheet: AuthService · UC-10 Login with Email &amp; Password.
    /// TC-UNIT-AuthService-030 → 035.
    /// Hai handler này khác LearnerLogin ở chỗ chúng KHÔNG trả ResponseDto:
    /// ExpertLogin trả <c>null</c>, AdminLogin ném <see cref="UnauthorizedAccessException"/>.
    /// </summary>
    public class ExpertAdminLoginHandlerTests
    {
        private readonly Mock<IUserRepository> _users = new();
        private readonly Mock<IPasswordHasher> _hasher = new();
        private readonly Mock<ITokenProvider> _tokens = new();
        private readonly Mock<IUnitOfWork> _uow = new();

        public ExpertAdminLoginHandlerTests()
        {
            _uow.Setup(u => u.Users).Returns(_users.Object);
            _tokens.Setup(t => t.GenerateAccessToken(It.IsAny<User>())).Returns("jwt");
            _tokens.Setup(t => t.GenerateRefreshToken()).Returns("refresh");
        }

        private ExpertLoginCommandHandler ExpertHandler()
            => new(_uow.Object, _hasher.Object, _tokens.Object);

        private AdminLoginCommandHandler AdminHandler()
            => new(_uow.Object, _hasher.Object, _tokens.Object);

        // ============================================================ ExpertLogin

        // ------------------------------------------------------------ TC-030
        // Covers: Main Flow.
        [Fact]
        [Trait("TC", "TC-UNIT-AuthService-030")]
        [Trait("UC", "UC-10")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Critical")]
        public async Task ExpertLogin_ValidCredentials_ReturnsTokens()
        {
            var user = new UserBuilder()
                .WithEmail("e@aila.com")
                .WithRole(UserRole.Expert)
                .WithPasswordHash("$2a$H")
                .Build();

            _users.Setup(r => r.GetByEmailAsync("e@aila.com")).ReturnsAsync(user);
            _hasher.Setup(h => h.Verify("secret", "$2a$H")).Returns(true);

            var result = await ExpertHandler()
                .Handle(new ExpertLoginCommand("e@aila.com", "secret"), CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("jwt", result!.AccessToken);
            Assert.Equal("refresh", result.RefreshToken);
            Assert.Equal("Expert", result.Role);
            Assert.Equal(user.Id, result.UserId);
            Assert.Equal(user.Email, result.Email);
            Assert.Equal(user.FullName, result.FullName);
        }

        // ------------------------------------------------------------ TC-031
        // Covers: BR-01 role gate. Role được kiểm tra TRƯỚC mật khẩu, nên một Learner
        // không thể dùng cổng expert để dò mật khẩu.
        [Theory]
        [InlineData(UserRole.Learner)]
        [InlineData(UserRole.Admin)]
        [Trait("TC", "TC-UNIT-AuthService-031")]
        [Trait("UC", "UC-10")]
        [Trait("BR", "BR-01")]
        [Trait("Type", "Security (GBR)")]
        [Trait("Priority", "Critical")]
        public async Task ExpertLogin_NonExpertRole_NullAndNoVerify(UserRole role)
        {
            var user = new UserBuilder().WithRole(role).WithPasswordHash("$2a$H").Build();
            _users.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);

            var result = await ExpertHandler()
                .Handle(new ExpertLoginCommand("x@aila.com", "secret"), CancellationToken.None);

            Assert.Null(result);
            _hasher.Verify(h => h.Verify(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _tokens.Verify(t => t.GenerateAccessToken(It.IsAny<User>()), Times.Never);
        }

        // ------------------------------------------------------------ TC-032
        // Covers: AF-01 — bốn nhánh cùng trả null để không lộ tài khoản nào tồn tại.
        [Fact]
        [Trait("TC", "TC-UNIT-AuthService-032")]
        [Trait("UC", "UC-10")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task ExpertLogin_UnknownInactiveOrWrongPassword_Null()
        {
            var handler = ExpertHandler();
            var cmd = new ExpertLoginCommand("e@aila.com", "secret");

            // (a) email không tồn tại
            _users.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);
            Assert.Null(await handler.Handle(cmd, CancellationToken.None));

            // (b) tài khoản bị khoá
            var locked = new UserBuilder().WithRole(UserRole.Expert).WithPasswordHash("$2a$H").Inactive().Build();
            _users.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(locked);
            Assert.Null(await handler.Handle(cmd, CancellationToken.None));

            // (c) tài khoản Google thuần, chưa có mật khẩu.
            // User bắt buộc có ít nhất một phương thức xác thực nên phải kèm GoogleId,
            // nếu không constructor ném InvalidOperationException.
            var noPassword = new UserBuilder()
                .WithRole(UserRole.Expert)
                .WithPasswordHash(null)
                .WithGoogleId("google-expert-1")
                .Build();
            _users.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(noPassword);
            Assert.Null(await handler.Handle(cmd, CancellationToken.None));

            // (d) sai mật khẩu
            var active = new UserBuilder().WithRole(UserRole.Expert).WithPasswordHash("$2a$H").Build();
            _users.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(active);
            _hasher.Setup(h => h.Verify("secret", "$2a$H")).Returns(false);
            Assert.Null(await handler.Handle(cmd, CancellationToken.None));

            _tokens.Verify(t => t.GenerateAccessToken(It.IsAny<User>()), Times.Never);
        }

        // ============================================================ AdminLogin

        // ------------------------------------------------------------ TC-033
        // Covers: Main Flow.
        [Fact]
        [Trait("TC", "TC-UNIT-AuthService-033")]
        [Trait("UC", "UC-10")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Critical")]
        public async Task AdminLogin_ValidCredentials_ReturnsTokens()
        {
            var user = new UserBuilder()
                .WithEmail("admin@aila.com")
                .WithRole(UserRole.Admin)
                .WithPasswordHash("$2a$H")
                .Build();

            _users.Setup(r => r.GetByEmailAsync("admin@aila.com")).ReturnsAsync(user);
            _hasher.Setup(h => h.Verify("secret", "$2a$H")).Returns(true);

            var result = await AdminHandler()
                .Handle(new AdminLoginCommand("admin@aila.com", "secret"), CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("jwt", result!.AccessToken);
            Assert.Equal("refresh", result.RefreshToken);
            Assert.Equal(user.Id, result.UserId);
        }

        // ------------------------------------------------------------ TC-034
        // Covers: AF-01 — cả ba nhánh đều ném UnauthorizedAccessException. Nhánh (b) mang
        // thông điệp khác hai nhánh còn lại, nên vẫn phân biệt được "sai mật khẩu" với
        // "bị khoá" nếu API trả message ra ngoài — cân nhắc ở tầng API.
        [Fact]
        [Trait("TC", "TC-UNIT-AuthService-034")]
        [Trait("UC", "UC-10")]
        [Trait("Type", "Boundary & Negative")]
        [Trait("Priority", "High")]
        public async Task AdminLogin_UnknownLockedOrWrongPassword_Throws()
        {
            var handler = AdminHandler();
            var cmd = new AdminLoginCommand("admin@aila.com", "secret");

            _users.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => handler.Handle(cmd, CancellationToken.None));

            var locked = new UserBuilder().WithRole(UserRole.Admin).WithPasswordHash("$2a$H").Inactive().Build();
            _users.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(locked);
            var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => handler.Handle(cmd, CancellationToken.None));
            Assert.Contains("khóa", ex.Message);

            var active = new UserBuilder().WithRole(UserRole.Admin).WithPasswordHash("$2a$H").Build();
            _users.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(active);
            _hasher.Setup(h => h.Verify("secret", "$2a$H")).Returns(false);
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => handler.Handle(cmd, CancellationToken.None));

            _tokens.Verify(t => t.GenerateAccessToken(It.IsAny<User>()), Times.Never);
        }

        // ------------------------------------------------------------ TC-035  ⚠ DEFECT
        // AdminLoginCommandHandler KHÔNG kiểm tra user.Role. Bất kỳ tài khoản nào đang active
        // và đúng mật khẩu đều lấy được token qua cổng admin — kể cả Learner. So sánh với
        // ExpertLoginCommandHandler vốn chặn bằng `user.Role != UserRole.Expert` (xem TC-031).
        // Token sinh ra mang role thật của user, nhưng việc cổng admin chấp nhận đăng nhập
        // đã là một lỗ hổng: nó xác nhận cặp email/mật khẩu cho kẻ tấn công.
        // Test khoá hành vi hiện tại; khi fix xong hãy đổi thành ThrowsAsync và bỏ Skip.
        [Fact(Skip = "DEF-AUTH-05 - AdminLogin does not check user.Role, any active account can sign in")]
        [Trait("TC", "TC-UNIT-AuthService-035")]
        [Trait("UC", "UC-10")]
        [Trait("BR", "BR-01")]
        [Trait("Type", "Security (GBR)")]
        [Trait("Priority", "Critical")]
        [Trait("Defect", "DEF-AUTH-05")]
        public async Task AdminLogin_LearnerAccount_SignsInBug()
        {
            var learner = new UserBuilder()
                .WithEmail("learner@aila.com")
                .WithRole(UserRole.Learner)
                .WithPasswordHash("$2a$H")
                .Build();

            _users.Setup(r => r.GetByEmailAsync("learner@aila.com")).ReturnsAsync(learner);
            _hasher.Setup(h => h.Verify("secret", "$2a$H")).Returns(true);

            var result = await AdminHandler()
                .Handle(new AdminLoginCommand("learner@aila.com", "secret"), CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("Learner", result!.Role);          // cổng admin cấp token cho Learner
            _tokens.Verify(t => t.GenerateAccessToken(learner), Times.Once);
        }

        // ------------------------------------------------------------ TC-036  ⚠ DEFECT
        // Handler gán cứng FullName = "Administrator" và Email = "adminEmail" (chuỗi literal,
        // không phải email của user). FE hiển thị sai và không có cách nào lấy email thật.
        [Fact(Skip = "DEF-AUTH-06 - AdminLogin returns the literal \"adminEmail\" instead of the real email")]
        [Trait("TC", "TC-UNIT-AuthService-036")]
        [Trait("UC", "UC-10")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        [Trait("Defect", "DEF-AUTH-06")]
        public async Task AdminLogin_ReturnsHardcodedIdentityBug()
        {
            var user = new UserBuilder()
                .WithEmail("admin@aila.com")
                .WithFullName("Tran Quan Tri")
                .WithRole(UserRole.Admin)
                .WithPasswordHash("$2a$H")
                .Build();

            _users.Setup(r => r.GetByEmailAsync("admin@aila.com")).ReturnsAsync(user);
            _hasher.Setup(h => h.Verify("secret", "$2a$H")).Returns(true);

            var result = await AdminHandler()
                .Handle(new AdminLoginCommand("admin@aila.com", "secret"), CancellationToken.None);

            // Mong đợi: "admin@aila.com" và "Tran Quan Tri".
            Assert.Equal("adminEmail", result!.Email);
            Assert.Equal("Administrator", result.FullName);
        }
    }
}
