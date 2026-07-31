using AILA.Application.Common.Interfaces;
using AILA.Application.Common.Interfaces.Repositories;
using AILA.Application.Features.Authentication.Commands.Register;
using AILA.Application.Tests.Common.Builders;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using Moq;

namespace AILA.Application.Tests.Features.Authentication
{
    /// <summary>
    /// Sheet: AuthService · Method Under Test: register(RegisterRequest) · UC-01.
    /// TC-UNIT-AuthService-001 → 006.
    /// </summary>
    public class RegisterCommandHandlerTests
    {
        private readonly Mock<IUserRepository> _users = new();
        private readonly Mock<ILearnerRepository> _learners = new();
        private readonly Mock<IPasswordHasher> _hasher = new();
        private readonly Mock<IUnitOfWork> _uow = new();
        private readonly RegisterCommandHandler _handler;

        public RegisterCommandHandlerTests()
        {
            _uow.Setup(u => u.Users).Returns(_users.Object);
            _uow.Setup(u => u.Learners).Returns(_learners.Object);
            _hasher.Setup(h => h.HashPassword(It.IsAny<string>())).Returns("$2a$HASH");

            _handler = new RegisterCommandHandler(_uow.Object, _hasher.Object);
        }

        private static RegisterCommand Command(
            string email = "new@aila.com",
            string fullName = "Nguyen An",
            string password = "Valid@123")
            => new() { Email = email, FullName = fullName, Password = password };

        private void VerifyNothingPersisted()
        {
            _users.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
            _learners.Verify(r => r.AddAsync(It.IsAny<Learner>()), Times.Never);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        // ------------------------------------------------------------ TC-001
        // Covers: Main Flow — tài khoản mới luôn là Learner và ở trạng thái active.
        [Fact]
        [Trait("TC", "TC-UNIT-AuthService-001")]
        [Trait("UC", "UC-01")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Critical")]
        public async Task Handle_ValidInput_CreatesActiveLearner()
        {
            _users.Setup(r => r.GetByEmailAsync("new@aila.com")).ReturnsAsync((User?)null);

            User? added = null;
            _users.Setup(r => r.AddAsync(It.IsAny<User>()))
                  .Callback<User>(u => added = u)
                  .Returns(Task.CompletedTask);

            var result = await _handler.Handle(Command(), CancellationToken.None);

            Assert.True(result.Success);
            Assert.NotEqual(Guid.Empty, result.Data!.UserId);
            Assert.NotNull(added);
            Assert.Equal(UserRole.Learner, added!.Role);
            Assert.True(added.IsActive);
            Assert.Equal(added.Id, result.Data.UserId);
            _users.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Once);
            _learners.Verify(r => r.AddAsync(It.IsAny<Learner>()), Times.Once);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        // ------------------------------------------------------------ TC-002
        // Covers: BR-01 email unique — nhánh FALSE. Không được ghi gì khi email đã tồn tại.
        [Fact]
        [Trait("TC", "TC-UNIT-AuthService-002")]
        [Trait("UC", "UC-01")]
        [Trait("BR", "BR-01")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task Handle_EmailExists_RejectedNoSave()
        {
            _users.Setup(r => r.GetByEmailAsync(It.IsAny<string>()))
                  .ReturnsAsync(new UserBuilder().WithEmail("dup@aila.com").Build());

            var result = await _handler.Handle(Command(email: "dup@aila.com"), CancellationToken.None);

            Assert.False(result.Success);
            Assert.Equal("EMAIL_EXISTS", result.ErrorCode);
            VerifyNothingPersisted();
        }

        // ------------------------------------------------------------ TC-003
        // Covers: AF-01 missing field. Domain là hàng rào duy nhất — không có validator cho
        // RegisterCommand, nên handler KHÔNG bắt ArgumentException và nó nổi ra ngoài.
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [Trait("TC", "TC-UNIT-AuthService-003")]
        [Trait("UC", "UC-01")]
        [Trait("Type", "Input Validation")]
        [Trait("Priority", "High")]
        public async Task Handle_BlankFullName_ThrowsFromDomain(string fullName)
        {
            _users.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => _handler.Handle(Command(fullName: fullName), CancellationToken.None));

            Assert.Equal("fullName", ex.ParamName);
            Assert.Contains("Tên người dùng không được để trống.", ex.Message);
            VerifyNothingPersisted();
        }

        // ------------------------------------------------------------ TC-004  ⚠ DEFECT
        // UC-01 Request Fields yêu cầu "Email must be a valid email format", nhưng luồng
        // Register KHÔNG kiểm định dạng ở bất kỳ đâu: không có RegisterCommandValidator, và
        // constructor User chỉ chặn rỗng. EmailHelper.IsValidFormat có sẵn nhưng chỉ được
        // luồng Reset Password dùng.
        // Test này khoá HÀNH VI HIỆN TẠI để thấy rõ lỗ hổng; sửa test khi defect được fix.
        [Theory(Skip = "DEF-AUTH-01 - Register does not validate the email format")]
        [InlineData("abc")]
        [InlineData("abc@")]
        [InlineData("@aila.com")]
        [InlineData("a b@aila.com")]
        [Trait("TC", "TC-UNIT-AuthService-004")]
        [Trait("UC", "UC-01")]
        [Trait("Type", "Input Validation")]
        [Trait("Priority", "Medium")]
        [Trait("Defect", "DEF-AUTH-01")]
        public async Task Handle_MalformedEmail_AcceptedBug(string email)
        {
            _users.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

            var result = await _handler.Handle(Command(email: email), CancellationToken.None);

            Assert.True(result.Success);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        // ------------------------------------------------------------ TC-005  ⚠ DEFECT
        // BR-02 yêu cầu password tuân thủ policy. Register KHÔNG kiểm policy: handler băm
        // thẳng chuỗi thô rồi truyền HASH vào constructor — constructor không bao giờ nhìn
        // thấy password gốc nên không thể kiểm được.
        // PasswordPolicy.Validate có sẵn nhưng chỉ luồng Reset Password dùng.
        [Theory(Skip = "DEF-AUTH-02 - Register does not enforce any password policy")]
        [InlineData("123")]
        [InlineData("abc")]
        [InlineData("1234567")]
        [Trait("TC", "TC-UNIT-AuthService-005")]
        [Trait("UC", "UC-01")]
        [Trait("BR", "BR-02")]
        [Trait("Type", "Input Validation")]
        [Trait("Priority", "High")]
        [Trait("Defect", "DEF-AUTH-02")]
        public async Task Handle_WeakPassword_AcceptedBug(string password)
        {
            _users.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

            var result = await _handler.Handle(Command(password: password), CancellationToken.None);

            Assert.True(result.Success);
            _hasher.Verify(h => h.HashPassword(password), Times.Once);
        }

        // ------------------------------------------------------------ TC-006
        // Covers: BR-02 encoder invoked. Password thô không bao giờ được lưu.
        [Fact]
        [Trait("TC", "TC-UNIT-AuthService-006")]
        [Trait("UC", "UC-01")]
        [Trait("BR", "BR-02")]
        [Trait("Type", "Security (GBR)")]
        [Trait("Priority", "High")]
        public async Task Handle_Always_StoresHashNotPlaintext()
        {
            _users.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);
            _hasher.Setup(h => h.HashPassword("Valid@123")).Returns("$2a$HASH");

            User? added = null;
            _users.Setup(r => r.AddAsync(It.IsAny<User>()))
                  .Callback<User>(u => added = u)
                  .Returns(Task.CompletedTask);

            await _handler.Handle(Command(password: "Valid@123"), CancellationToken.None);

            Assert.Equal("$2a$HASH", added!.PasswordHash);
            Assert.NotEqual("Valid@123", added.PasswordHash);
            _hasher.Verify(h => h.HashPassword("Valid@123"), Times.Once);
        }
    }
}
