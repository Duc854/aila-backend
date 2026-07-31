using AILA.Application.Common.Interfaces;
using AILA.Application.Common.Interfaces.Repositories;
using AILA.Application.Features.Authentication.Commands.GoogleCallback;
using AILA.Application.Tests.Common.Builders;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;

namespace AILA.Application.Tests.Features.Authentication
{
    /// <summary>
    /// Sheet: AuthService · UC-15 Login with Google SSO (nhánh authorization-code).
    /// TC-UNIT-AuthService-037 → 040.
    /// Khác <c>GoogleLoginCommand</c> (nhận thẳng id-token), handler này phải đổi
    /// authorization code lấy id-token trước, nên có thêm một nhánh lỗi ở đầu.
    /// </summary>
    public class GoogleCallbackCommandHandlerTests
    {
        private readonly Mock<IUserRepository> _users = new();
        private readonly Mock<ILearnerRepository> _learners = new();
        private readonly Mock<IUnitOfWork> _uow = new();
        private readonly Mock<IGoogleAuthService> _google = new();
        private readonly Mock<ITokenProvider> _tokens = new();
        private readonly Mock<ILogger<GoogleCallbackCommandHandler>> _logger = new();
        private readonly GoogleCallbackCommandHandler _handler;

        public GoogleCallbackCommandHandlerTests()
        {
            _uow.Setup(u => u.Users).Returns(_users.Object);
            _uow.Setup(u => u.Learners).Returns(_learners.Object);
            _tokens.Setup(t => t.GenerateAccessToken(It.IsAny<User>())).Returns("jwt");
            _tokens.Setup(t => t.GenerateRefreshToken()).Returns("refresh");

            _handler = new GoogleCallbackCommandHandler(
                _uow.Object, _google.Object, _tokens.Object, _logger.Object);
        }

        private static GoogleCallbackCommand Command(string code = "auth-code")
            => new() { AuthorizationCode = code, RedirectUri = "https://aila.com/callback" };

        private static GoogleTokenPayload Payload(
            string email = "g@aila.com",
            string? googleId = "google-123",
            string? picture = "https://cdn/pic.png")
            => new() { Email = email, Name = "Google User", GoogleId = googleId, Picture = picture };

        private void ExchangeReturns(string? idToken)
            => _google.Setup(g => g.ExchangeCodeForIdTokenAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(idToken);

        private void VerifyReturns(GoogleTokenPayload? payload)
            => _google.Setup(g => g.VerifyGoogleTokenAsync(It.IsAny<string>())).ReturnsAsync(payload);

        // ------------------------------------------------------------ TC-037
        // Covers: Main Flow — người dùng mới. Handler tự tạo cả User lẫn Learner rồi mới
        // phát token, nên phải kiểm tra cả hai AddAsync chứ không chỉ token.
        [Fact]
        [Trait("TC", "TC-UNIT-AuthService-036")]
        [Trait("UC", "UC-15")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Critical")]
        public async Task Handle_NewUser_CreatesUserAndLearner()
        {
            ExchangeReturns("id-token");
            VerifyReturns(Payload());
            _users.Setup(r => r.GetByEmailAsync("g@aila.com")).ReturnsAsync((User?)null);

            User? added = null;
            _users.Setup(r => r.AddAsync(It.IsAny<User>()))
                  .Callback<User>(u => added = u)
                  .Returns(Task.CompletedTask);

            var result = await _handler.Handle(Command(), CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal("jwt", result.Data!.AccessToken);
            Assert.Equal("Learner", result.Data.Role);
            Assert.NotNull(added);
            Assert.Equal("g@aila.com", added!.Email);
            Assert.Equal(UserRole.Learner, added.Role);
            Assert.Equal("google-123", added.GoogleId);
            _learners.Verify(r => r.AddAsync(It.IsAny<Learner>()), Times.Once);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        // ------------------------------------------------------------ TC-038
        // Covers: Main Flow — người dùng đã có. Lần đầu đăng nhập bằng Google thì GoogleId
        // được gắn vào tài khoản email/mật khẩu sẵn có.
        [Fact]
        [Trait("TC", "TC-UNIT-AuthService-037")]
        [Trait("UC", "UC-15")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task Handle_ExistingLearner_LinksGoogleIdAndSaves()
        {
            var existing = new UserBuilder()
                .WithEmail("g@aila.com")
                .WithRole(UserRole.Learner)
                .WithGoogleId(null)
                .Build();

            ExchangeReturns("id-token");
            VerifyReturns(Payload());
            _users.Setup(r => r.GetByEmailAsync("g@aila.com")).ReturnsAsync(existing);

            var result = await _handler.Handle(Command(), CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal("google-123", existing.GoogleId);
            _users.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
            _learners.Verify(r => r.AddAsync(It.IsAny<Learner>()), Times.Never);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        // ------------------------------------------------------------ TC-039
        // Covers: BR-01 — tài khoản Expert/Admin không được vào bằng cổng Google (chỉ Learner).
        [Theory]
        [InlineData(UserRole.Expert)]
        [InlineData(UserRole.Admin)]
        [Trait("TC", "TC-UNIT-AuthService-037")]
        [Trait("UC", "UC-15")]
        [Trait("BR", "BR-01")]
        [Trait("Type", "Security (GBR)")]
        [Trait("Priority", "High")]
        public async Task Handle_ExistingNonLearner_InvalidRole(UserRole role)
        {
            var existing = new UserBuilder().WithEmail("g@aila.com").WithRole(role).Build();

            ExchangeReturns("id-token");
            VerifyReturns(Payload());
            _users.Setup(r => r.GetByEmailAsync("g@aila.com")).ReturnsAsync(existing);

            var result = await _handler.Handle(Command(), CancellationToken.None);

            Assert.False(result.Success);
            Assert.Equal("INVALID_ROLE", result.ErrorCode);
            _tokens.Verify(t => t.GenerateAccessToken(It.IsAny<User>()), Times.Never);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        // ------------------------------------------------------------ TC-040
        // Covers: AF-01 — ba nhánh lỗi trước khi chạm tới database.
        [Fact]
        [Trait("TC", "TC-UNIT-AuthService-037")]
        [Trait("UC", "UC-15")]
        [Trait("Type", "Boundary & Negative")]
        [Trait("Priority", "High")]
        public async Task Handle_BadCodeOrToken_FailsBeforeDatabase()
        {
            // (a) đổi code thất bại
            _google.Setup(g => g.ExchangeCodeForIdTokenAsync(
                        It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                   .ThrowsAsync(new InvalidOperationException("bad code"));
            var a = await _handler.Handle(Command(), CancellationToken.None);
            Assert.False(a.Success);
            Assert.Equal("INVALID_GOOGLE_CODE", a.ErrorCode);

            // (b) Google trả id-token rỗng
            ExchangeReturns("   ");
            var b = await _handler.Handle(Command(), CancellationToken.None);
            Assert.False(b.Success);
            Assert.Equal("INVALID_GOOGLE_CODE", b.ErrorCode);

            // (c) id-token không xác thực được
            ExchangeReturns("id-token");
            VerifyReturns(null);
            var c = await _handler.Handle(Command(), CancellationToken.None);
            Assert.False(c.Success);
            Assert.Equal("INVALID_GOOGLE_TOKEN", c.ErrorCode);

            _users.Verify(r => r.GetByEmailAsync(It.IsAny<string>()), Times.Never);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
