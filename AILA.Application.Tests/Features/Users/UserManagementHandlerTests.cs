using AILA.Application.Common.Interfaces;
using AILA.Application.Common.Interfaces.Repositories;
using AILA.Application.Features.Users.Commands.CreateExpertAccount;
using AILA.Application.Features.Users.Commands.UpdateUserStatus;
using AILA.Application.Features.Users.Queries.GetUserById;
using AILA.Application.Features.Users.Queries.GetUsers;
using AILA.Application.Tests.Common.Builders;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using Moq;

namespace AILA.Application.Tests.Features.Users
{
    /// <summary>
    /// Sheet: UserService · UC-76 / UC-77 / UC-78 · TC-UNIT-UserService-001 → 015.
    /// </summary>
    public class UserManagementHandlerTests
    {
        private readonly Mock<IUserRepository> _users = new();
        private readonly Mock<IPasswordHasher> _hasher = new();
        private readonly Mock<IUnitOfWork> _uow = new();

        public UserManagementHandlerTests()
        {
            _uow.Setup(u => u.Users).Returns(_users.Object);
        }

        private GetUsersQueryHandler GetUsersHandler() => new(_uow.Object);
        private GetUserByIdQueryHandler GetUserByIdHandler() => new(_uow.Object);
        private UpdateUserStatusCommandHandler UpdateStatusHandler() => new(_uow.Object);
        private CreateExpertAccountCommandHandler CreateExpertHandler() => new(_uow.Object, _hasher.Object);

        // ============================================================ TC-001
        // Covers: Main Flow — map đủ field sang UserListDto, không rơi rớt bản ghi nào.
        [Fact]
        [Trait("TC", "TC-UNIT-UserService-001")]
        [Trait("UC", "UC-76")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task GetUsers_NoFilter_MapsEveryUserToDto()
        {
            var learner = new UserBuilder().WithEmail("a@aila.com").WithFullName("An").Build();
            var expert = new UserBuilder().WithEmail("b@aila.com").WithFullName("Binh")
                                          .WithRole(UserRole.Expert).Inactive().Build();
            _users.Setup(r => r.GetUsersAsync(null, null, null, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(new List<User> { learner, expert });

            var result = await GetUsersHandler().Handle(new GetUsersQuery(), CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal(2, result.Data!.Count);

            var first = result.Data[0];
            Assert.Equal(learner.Id, first.Id);
            Assert.Equal("a@aila.com", first.Email);
            Assert.Equal("An", first.FullName);
            Assert.Equal(UserRole.Learner, first.Role);
            Assert.True(first.IsActive);
            Assert.Equal(learner.CreatedAt, first.CreatedAt);

            Assert.Equal(UserRole.Expert, result.Data[1].Role);
            Assert.False(result.Data[1].IsActive);
        }

        // ============================================================ TC-002 → 005
        // Covers: BR-02 keyword, BR-03 role/status filter và tổ hợp.
        // Phạm vi L1: việc LỌC thực sự nằm ở UserRepository.BuildFilterQuery (Contains/ToLower
        // dịch sang SQL) nên không chạm tới được khi đã mock repo. Ở đây chỉ khẳng định handler
        // TRUYỀN ĐÚNG tham số xuống — phần lọc thuộc integration test L2 của repository.
        public static TheoryData<string?, UserRole?, bool?> FilterCombinations => new()
        {
            { "nguyen", null,              null },   // TC-002
            { null,     UserRole.Learner,  null },   // TC-003
            { null,     null,              true },   // TC-004
            { "nguyen", UserRole.Learner,  true },   // TC-005
        };

        [Theory]
        [MemberData(nameof(FilterCombinations))]
        [Trait("TC", "TC-UNIT-UserService-002")]
        [Trait("UC", "UC-76")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        public async Task GetUsers_WithFilters_ForwardsArgs(
            string? keyword, UserRole? role, bool? isActive)
        {
            _users.Setup(r => r.GetUsersAsync(keyword, role, isActive, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(new List<User>());

            var result = await GetUsersHandler().Handle(
                new GetUsersQuery(keyword, role, isActive), CancellationToken.None);

            Assert.True(result.Success);
            _users.Verify(r => r.GetUsersAsync(keyword, role, isActive, It.IsAny<CancellationToken>()), Times.Once);
        }

        // ============================================================ TC-006
        // Covers: AF-01 empty result — không khớp gì là danh sách rỗng, KHÔNG phải lỗi.
        [Fact]
        [Trait("TC", "TC-UNIT-UserService-003")]
        [Trait("UC", "UC-76")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        public async Task GetUsers_NoMatch_ReturnsSuccessWithEmptyList()
        {
            _users.Setup(r => r.GetUsersAsync("xyz123", null, null, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(new List<User>());

            var result = await GetUsersHandler().Handle(
                new GetUsersQuery("xyz123"), CancellationToken.None);

            Assert.True(result.Success);
            Assert.Empty(result.Data!);
            Assert.Null(result.ErrorCode);
        }

        // ============================================================ TC-007
        // Covers: BR-04 exclude admin.
        // KHẲNG ĐỊNH CÓ CHỦ ĐÍCH: handler là pass-through, nó KHÔNG tự lọc Admin. Nếu repository
        // trả về Admin thì Admin sẽ lọt ra ngoài. BR-04 được thực thi ở
        // UserRepository.BuildFilterQuery (query.Where(u => u.Role != UserRole.Admin)).
        // Test này ghim ranh giới đó, để ai đó tưởng handler có lọc thì thấy ngay là không.
        // ⇒ Việc chứng minh BR-04 thật sự phải nằm ở L2 test của UserRepository.
        [Fact]
        [Trait("TC", "TC-UNIT-UserService-004")]
        [Trait("UC", "UC-76")]
        [Trait("BR", "BR-04")]
        [Trait("Type", "Security (GBR)")]
        [Trait("Priority", "Medium")]
        public async Task GetUsers_AdminFilterIsRepoLevel()
        {
            var admin = new UserBuilder().WithEmail("admin@aila.com").WithRole(UserRole.Admin).Build();
            _users.Setup(r => r.GetUsersAsync(null, null, null, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(new List<User> { admin });

            var result = await GetUsersHandler().Handle(new GetUsersQuery(), CancellationToken.None);

            Assert.True(result.Success);
            Assert.Contains(result.Data!, u => u.Role == UserRole.Admin);
        }

        // ============================================================ TC-008
        // Covers: BR-01 detail fields — happy path map đủ field.
        [Fact]
        [Trait("TC", "TC-UNIT-UserService-008")]
        [Trait("UC", "UC-76")]
        [Trait("BR", "BR-01")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        public async Task GetUserById_Existing_AllDetailFields()
        {
            var user = new UserBuilder().WithEmail("a@aila.com").WithFullName("An").Build();
            _users.Setup(r => r.GetUserWithDetailsAsync(user.Id, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(user);

            var result = await GetUserByIdHandler().Handle(
                new GetUserByIdQuery(user.Id), CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal(user.Id, result.Data!.Id);
            Assert.Equal("a@aila.com", result.Data.Email);
            Assert.Equal("An", result.Data.FullName);
            Assert.Equal(UserRole.Learner, result.Data.Role);
            Assert.True(result.Data.IsActive);
            Assert.Equal(user.CreatedAt, result.Data.CreatedAt);
            Assert.Equal(user.UpdatedAt, result.Data.UpdatedAt);
        }

        // ------------------------------------------------------------ TC-008 (nhánh lỗi)
        [Fact]
        [Trait("TC", "TC-UNIT-UserService-008")]
        [Trait("UC", "UC-76")]
        [Trait("Type", "Input Validation")]
        [Trait("Priority", "Medium")]
        public async Task GetUserById_EmptyGuid_RejectedNoDb()
        {
            var result = await GetUserByIdHandler().Handle(
                new GetUserByIdQuery(Guid.Empty), CancellationToken.None);

            Assert.Equal("INVALID_USER_ID", result.ErrorCode);
            _users.Verify(r => r.GetUserWithDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        [Trait("TC", "TC-UNIT-UserService-008")]
        [Trait("UC", "UC-76")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        public async Task GetUserById_NotFound_ReturnsUserNotFound()
        {
            _users.Setup(r => r.GetUserWithDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync((User?)null);

            var result = await GetUserByIdHandler().Handle(
                new GetUserByIdQuery(Guid.NewGuid()), CancellationToken.None);

            Assert.Equal("USER_NOT_FOUND", result.ErrorCode);
        }

        // BR-04 ở nhánh chi tiết thì handler CÓ tự chặn (khác với danh sách ở TC-007).
        [Fact]
        [Trait("TC", "TC-UNIT-UserService-008")]
        [Trait("UC", "UC-76")]
        [Trait("BR", "BR-04")]
        [Trait("Type", "Security (GBR)")]
        [Trait("Priority", "High")]
        public async Task GetUserById_AdminAccount_ReturnsAccessDenied()
        {
            var admin = new UserBuilder().WithRole(UserRole.Admin).Build();
            _users.Setup(r => r.GetUserWithDetailsAsync(admin.Id, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(admin);

            var result = await GetUserByIdHandler().Handle(
                new GetUserByIdQuery(admin.Id), CancellationToken.None);

            Assert.False(result.Success);
            Assert.Equal("ACCESS_DENIED", result.ErrorCode);
            Assert.Null(result.Data);
        }

        // ============================================================ TC-009 / TC-010
        // Covers: BR-01 state transition. Code dùng cờ bool IsActive chứ không có enum
        // SUSPENDED — "SUSPENDED" của UCS tương ứng IsActive=false.
        [Theory]
        [InlineData(true, false)]   // TC-009: ACTIVE → SUSPENDED
        [InlineData(false, true)]   // TC-010: SUSPENDED → ACTIVE
        [Trait("TC", "TC-UNIT-UserService-009")]
        [Trait("UC", "UC-77")]
        [Trait("BR", "BR-01")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task UpdateStatus_ValidTransition_FlipsFlag(
            bool startsActive, bool target)
        {
            var user = new UserBuilder().WithActive(startsActive).Build();
            _users.Setup(r => r.GetUserByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

            var result = await UpdateStatusHandler().Handle(
                new UpdateUserStatusCommand(user.Id, target), CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal(target, user.IsActive);
            Assert.Equal(target, result.Data!.IsActive);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        // ------------------------------------------------------------ TC-016 (MỚI)
        // Ba nhánh chặn của UpdateUserStatus không có TC nào trong workbook phủ.
        // → cần thêm dòng TC-UNIT-UserService-016 vào sheet UserService.
        [Fact]
        [Trait("TC", "TC-UNIT-UserService-010")]
        [Trait("UC", "UC-77")]
        [Trait("Type", "Input Validation")]
        [Trait("Priority", "Medium")]
        public async Task UpdateStatus_EmptyGuid_RejectedNoSave()
        {
            var result = await UpdateStatusHandler().Handle(
                new UpdateUserStatusCommand(Guid.Empty, true), CancellationToken.None);

            Assert.Equal("INVALID_USER_ID", result.ErrorCode);
            _users.Verify(r => r.GetUserByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        [Trait("TC", "TC-UNIT-UserService-016")]
        [Trait("UC", "UC-77")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        public async Task UpdateStatus_NotFound_RejectedNoSave()
        {
            _users.Setup(r => r.GetUserByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync((User?)null);

            var result = await UpdateStatusHandler().Handle(
                new UpdateUserStatusCommand(Guid.NewGuid(), false), CancellationToken.None);

            Assert.Equal("USER_NOT_FOUND", result.ErrorCode);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        // Admin không được khoá qua UC-77 — nếu không, một admin có thể tự khoá hết admin còn lại.
        [Fact]
        [Trait("TC", "TC-UNIT-UserService-016")]
        [Trait("UC", "UC-77")]
        [Trait("Type", "Security (GBR)")]
        [Trait("Priority", "High")]
        public async Task UpdateStatus_Admin_DeniedKeepsStatus()
        {
            var admin = new UserBuilder().WithRole(UserRole.Admin).Build();
            _users.Setup(r => r.GetUserByIdAsync(admin.Id, It.IsAny<CancellationToken>())).ReturnsAsync(admin);

            var result = await UpdateStatusHandler().Handle(
                new UpdateUserStatusCommand(admin.Id, false), CancellationToken.None);

            Assert.Equal("ACCESS_DENIED", result.ErrorCode);
            Assert.True(admin.IsActive);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        // ============================================================ TC-013
        // Covers: Main Flow / BR-03 — tài khoản Expert tạo ra phải Role=Expert, active,
        // và được lưu cùng hồ sơ Expert trong một transaction của repository.
        [Fact]
        [Trait("TC", "TC-UNIT-UserService-013")]
        [Trait("UC", "UC-78")]
        [Trait("BR", "BR-03")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task CreateExpert_Valid_ActiveInOneTx()
        {
            _users.Setup(r => r.EmailExistsAsync("expert@aila.com", It.IsAny<CancellationToken>()))
                  .ReturnsAsync(false);
            _hasher.Setup(h => h.HashPassword("Pass123")).Returns("$2a$H");

            User? savedUser = null;
            Expert? savedExpert = null;
            _users.Setup(r => r.CreateUserWithExpertProfileAsync(
                      It.IsAny<User>(), It.IsAny<Expert>(), It.IsAny<CancellationToken>()))
                  .Callback<User, Expert, CancellationToken>((u, e, _) => { savedUser = u; savedExpert = e; })
                  .ReturnsAsync((User u, Expert _, CancellationToken __) => u);

            var result = await CreateExpertHandler().Handle(
                new CreateExpertAccountCommand("Dr An", "Expert@AILA.com", "Pass123"),
                CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal(UserRole.Expert, result.Data!.Role);
            Assert.True(result.Data.IsActive);
            Assert.NotNull(savedUser);
            Assert.Equal("expert@aila.com", savedUser!.Email);   // đã normalize về chữ thường
            Assert.Equal("$2a$H", savedUser.PasswordHash);
            Assert.NotNull(savedExpert);
            Assert.Equal(savedUser.Id, savedExpert!.UserId);
            _users.Verify(r => r.CreateUserWithExpertProfileAsync(
                It.IsAny<User>(), It.IsAny<Expert>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        // ============================================================ TC-014
        // Covers: AF-01 missing field. Thứ tự kiểm có ý nghĩa — chỉ mã lỗi của field sai
        // ĐẦU TIÊN được trả về, vì FE hiển thị lỗi theo từng field.
        [Theory]
        [InlineData("", "expert@aila.com", "Pass123", "INVALID_FULL_NAME")]
        [InlineData("   ", "expert@aila.com", "Pass123", "INVALID_FULL_NAME")]
        [InlineData("Dr An", "", "Pass123", "INVALID_EMAIL")]
        [InlineData("Dr An", "not-an-email", "Pass123", "INVALID_EMAIL")]
        [InlineData("Dr An", "expert@aila.com", "", "INVALID_PASSWORD")]
        [InlineData("Dr An", "expert@aila.com", "12345", "INVALID_PASSWORD")]   // 5 ký tự — biên - 1
        [InlineData("", "not-an-email", "12345", "INVALID_FULL_NAME")]          // sai cả 3 → báo field đầu
        [Trait("TC", "TC-UNIT-UserService-014")]
        [Trait("UC", "UC-78")]
        [Trait("Type", "Input Validation")]
        [Trait("Priority", "High")]
        public async Task CreateExpert_InvalidField_FirstError(
            string fullName, string email, string password, string expectedCode)
        {
            var result = await CreateExpertHandler().Handle(
                new CreateExpertAccountCommand(fullName, email, password), CancellationToken.None);

            Assert.False(result.Success);
            Assert.Equal(expectedCode, result.ErrorCode);
            _users.Verify(r => r.EmailExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            _users.Verify(r => r.CreateUserWithExpertProfileAsync(
                It.IsAny<User>(), It.IsAny<Expert>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        // Biên dưới của policy password: đúng 6 ký tự phải được chấp nhận.
        [Fact]
        [Trait("TC", "TC-UNIT-UserService-015")]
        [Trait("UC", "UC-78")]
        [Trait("Type", "Boundary & Negative")]
        [Trait("Priority", "Medium")]
        public async Task CreateExpert_PasswordAtMinLength_Ok()
        {
            _users.Setup(r => r.EmailExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(false);
            _hasher.Setup(h => h.HashPassword(It.IsAny<string>())).Returns("$2a$H");
            _users.Setup(r => r.CreateUserWithExpertProfileAsync(
                      It.IsAny<User>(), It.IsAny<Expert>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync((User u, Expert _, CancellationToken __) => u);

            var result = await CreateExpertHandler().Handle(
                new CreateExpertAccountCommand("Dr An", "expert@aila.com", "123456"), CancellationToken.None);

            Assert.True(result.Success);
        }

        // ============================================================ TC-015
        // Covers: BR-02 email unique.
        [Fact]
        [Trait("TC", "TC-UNIT-UserService-015")]
        [Trait("UC", "UC-78")]
        [Trait("BR", "BR-02")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task CreateExpert_DuplicateEmail_NoCreate()
        {
            _users.Setup(r => r.EmailExistsAsync("dup@aila.com", It.IsAny<CancellationToken>()))
                  .ReturnsAsync(true);

            var result = await CreateExpertHandler().Handle(
                new CreateExpertAccountCommand("Dr An", "dup@aila.com", "Pass123"), CancellationToken.None);

            Assert.False(result.Success);
            Assert.Equal("DUPLICATE_EMAIL", result.ErrorCode);
            _users.Verify(r => r.CreateUserWithExpertProfileAsync(
                It.IsAny<User>(), It.IsAny<Expert>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
