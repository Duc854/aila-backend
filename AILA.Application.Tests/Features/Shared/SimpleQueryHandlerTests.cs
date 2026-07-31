using AILA.Application.Common.Interfaces;
using AILA.Application.Common.Interfaces.Repositories;
using AILA.Application.Features.Categories.Queries;
using AILA.Application.Features.Categories.Queries.GetCategories;
using AILA.Application.Features.Courses.Queries;
using AILA.Application.Features.Modules.Queries;
using AILA.Application.Features.Users.Queries.GetCurrentUser;
using AILA.Application.Features.Users.Queries.GetRoles;
using AILA.Application.Tests.Common.Builders;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using Moq;

namespace AILA.Application.Tests.Features.Shared
{
    /// <summary>
    /// Gom các query nhỏ trải trên nhiều sheet:
    /// UserService (UC-13, UC-73) · ModuleService (UC-42) · CourseService (UC-22) ·
    /// CategoryService (UC-78, UC-03).
    /// TC-UNIT-UserService-017→020 · ModuleService-013,014 · CourseService-030,031 ·
    /// CategoryService-014→017.
    /// </summary>
    public class SimpleQueryHandlerTests
    {
        private static readonly Guid OwnerId = Guid.NewGuid();
        private static readonly Guid OtherExpertId = Guid.NewGuid();

        private readonly Mock<IUserRepository> _users = new();
        private readonly Mock<ICourseRepository> _courses = new();
        private readonly Mock<IModuleRepository> _modules = new();
        private readonly Mock<IEnrollmentRepository> _enrollments = new();
        private readonly Mock<ICategoryRepository> _categories = new();
        private readonly Mock<IUnitOfWork> _uow = new();

        public SimpleQueryHandlerTests()
        {
            _uow.Setup(u => u.Users).Returns(_users.Object);
            _uow.Setup(u => u.Courses).Returns(_courses.Object);
            _uow.Setup(u => u.Modules).Returns(_modules.Object);
            _uow.Setup(u => u.Enrollments).Returns(_enrollments.Object);
            _uow.Setup(u => u.Categories).Returns(_categories.Object);
        }

        // ============================================================ UC-13 GetCurrentUser

        // ------------------------------------------------------------ TC-UserService-017
        [Fact]
        [Trait("TC", "TC-UNIT-UserService-017")]
        [Trait("UC", "UC-13")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task CurrentUser_Existing_ReturnsProfile()
        {
            var user = new UserBuilder()
                .WithEmail("me@aila.com")
                .WithFullName("Nguyen An")
                .WithRole(UserRole.Learner)
                .Build();
            _users.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);

            var handler = new GetCurrentUserQueryHandler(_uow.Object);
            var result = await handler.Handle(
                new GetCurrentUserQuery { UserId = user.Id }, CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal("me@aila.com", result.Data!.Email);
            Assert.Equal("Nguyen An", result.Data.FullName);
            Assert.Equal("Learner", result.Data.Role);
            Assert.True(result.Data.IsActive);
        }

        // ------------------------------------------------------------ TC-UserService-018
        // Covers: AF-01 — token còn hạn nhưng tài khoản đã bị xoá.
        [Fact]
        [Trait("TC", "TC-UNIT-UserService-018")]
        [Trait("UC", "UC-13")]
        [Trait("Type", "Boundary & Negative")]
        [Trait("Priority", "Medium")]
        public async Task CurrentUser_Missing_NotFound()
        {
            _users.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);

            var handler = new GetCurrentUserQueryHandler(_uow.Object);
            var result = await handler.Handle(
                new GetCurrentUserQuery { UserId = Guid.NewGuid() }, CancellationToken.None);

            Assert.False(result.Success);
            Assert.Equal("USER_NOT_FOUND", result.ErrorCode);
        }

        // ------------------------------------------------------------ TC-UserService-019
        // Covers: BR-01 — danh sách vai trò cho form tạo tài khoản. Handler trả CỐ ĐỊNH
        // Expert + Learner, KHÔNG lấy từ enum UserRole, nên Admin không lọt vào form.
        // Đó là chủ đích: admin không được tạo admin khác qua màn hình này.
        [Fact]
        [Trait("TC", "TC-UNIT-UserService-019")]
        [Trait("UC", "UC-73")]
        [Trait("BR", "BR-01")]
        [Trait("Type", "Security (GBR)")]
        [Trait("Priority", "Medium")]
        public async Task Roles_ExcludesAdmin()
        {
            var handler = new GetRolesQueryHandler();
            var result = await handler.Handle(new GetRolesQuery(), CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal(2, result.Data!.Count);
            Assert.Contains(result.Data, r => r.Value == UserRole.Expert);
            Assert.Contains(result.Data, r => r.Value == UserRole.Learner);
            Assert.DoesNotContain(result.Data, r => r.Value == UserRole.Admin);
        }

        // ------------------------------------------------------------ TC-UserService-020
        // Danh sách cứng nên không đụng database — khẳng định để nếu ai đó đổi sang
        // tra bảng Role thì test này nhắc phải xem lại việc loại Admin ở TC-019.
        [Fact]
        [Trait("TC", "TC-UNIT-UserService-020")]
        [Trait("UC", "UC-73")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Low")]
        public async Task Roles_NoDatabaseAccess()
        {
            var handler = new GetRolesQueryHandler();
            await handler.Handle(new GetRolesQuery(), CancellationToken.None);

            _users.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
            _uow.VerifyNoOtherCalls();
        }

        // ============================================================ UC-42 GetModulesByCourse

        // ------------------------------------------------------------ TC-ModuleService-013
        [Fact]
        [Trait("TC", "TC-UNIT-ModuleService-013")]
        [Trait("UC", "UC-42")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task Modules_OwnCourse_ReturnsList()
        {
            var course = new CourseBuilder().OwnedBy(OwnerId).Build();
            _courses.Setup(r => r.GetByIdAsync(course.Id)).ReturnsAsync(course);
            _modules.Setup(r => r.GetByCourseIdAsync(course.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<Module>
                    {
                        new(course.Id, "Chương 1", 1),
                        new(course.Id, "Chương 2", 2),
                    });

            var handler = new GetModulesByCourseQueryHandler(_uow.Object);
            var result = await handler.Handle(
                new GetModulesByCourseQuery(course.Id, OwnerId), CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal(2, result.Data!.Count);
            Assert.Equal(0, result.Data[0].MaterialCount);
        }

        // ------------------------------------------------------------ TC-ModuleService-014
        // Covers: BR-01 — cấu trúc khoá học của expert khác là tài sản riêng, không được lộ.
        [Fact]
        [Trait("TC", "TC-UNIT-ModuleService-014")]
        [Trait("UC", "UC-42")]
        [Trait("BR", "BR-01")]
        [Trait("Type", "Security (GBR)")]
        [Trait("Priority", "Critical")]
        public async Task Modules_MissingOrForeign_Rejected()
        {
            var handler = new GetModulesByCourseQueryHandler(_uow.Object);

            _courses.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Course?)null);
            var missing = await handler.Handle(
                new GetModulesByCourseQuery(Guid.NewGuid(), OwnerId), CancellationToken.None);
            Assert.Equal("COURSE_NOT_FOUND", missing.ErrorCode);

            var foreign = new CourseBuilder().OwnedBy(OtherExpertId).Build();
            _courses.Setup(r => r.GetByIdAsync(foreign.Id)).ReturnsAsync(foreign);
            var denied = await handler.Handle(
                new GetModulesByCourseQuery(foreign.Id, OwnerId), CancellationToken.None);
            Assert.Equal("FORBIDDEN", denied.ErrorCode);

            _modules.Verify(r => r.GetByCourseIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        // ============================================================ UC-22 CheckEnrollment

        // ------------------------------------------------------------ TC-CourseService-030
        [Fact]
        [Trait("TC", "TC-UNIT-CourseService-030")]
        [Trait("UC", "UC-22")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task CheckEnrollment_Enrolled_ReportsDetails()
        {
            var learnerId = Guid.NewGuid();
            var courseId = Guid.NewGuid();
            var enrollment = new Enrollment(learnerId, courseId, 10);

            _enrollments.Setup(r => r.GetByLearnerAndCourseAsync(learnerId, courseId))
                        .ReturnsAsync(enrollment);

            var handler = new CheckEnrollmentQueryHandler(_uow.Object);
            var result = await handler.Handle(
                new CheckEnrollmentQuery(courseId, learnerId), CancellationToken.None);

            Assert.True(result.Success);
            Assert.True(result.Data!.IsEnrolled);
            Assert.Equal(enrollment.EnrolledAt, result.Data.EnrolledAt);
            Assert.Equal(enrollment.Status.ToString(), result.Data.Status);
        }

        // ------------------------------------------------------------ TC-CourseService-031
        // Covers: AF-01 — "chưa ghi danh" là câu trả lời BÌNH THƯỜNG (Success=true),
        // không phải lỗi; nút Enrol/Continue trên giao diện dựa vào đây.
        [Fact]
        [Trait("TC", "TC-UNIT-CourseService-031")]
        [Trait("UC", "UC-22")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task CheckEnrollment_NotEnrolled_SuccessWithFalse()
        {
            _enrollments.Setup(r => r.GetByLearnerAndCourseAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
                        .ReturnsAsync((Enrollment?)null);

            var handler = new CheckEnrollmentQueryHandler(_uow.Object);
            var result = await handler.Handle(
                new CheckEnrollmentQuery(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

            Assert.True(result.Success);
            Assert.False(result.Data!.IsEnrolled);
            Assert.Null(result.Data.EnrolledAt);
            Assert.Null(result.Data.Status);
        }

        // ============================================================ UC-78 / UC-03 Categories

        // ------------------------------------------------------------ TC-CategoryService-014
        // Covers: Main Flow — danh sách quản trị hiển thị CẢ danh mục đang tắt, kèm cờ IsActive.
        [Fact]
        [Trait("TC", "TC-UNIT-CategoryService-014")]
        [Trait("UC", "UC-78")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task Categories_Admin_IncludesInactive()
        {
            var visible = new CategoryBuilder().WithName("AI cơ bản").WithOrderIndex(1).Build();
            var hidden = new CategoryBuilder().WithName("Sắp ra mắt").WithOrderIndex(2).Build();

            _categories.Setup(r => r.GetAllOrderedAsync(It.IsAny<CancellationToken>()))
                       .ReturnsAsync(new List<Category> { visible, hidden });

            var handler = new GetCategoriesQueryHandler(_uow.Object);
            var result = await handler.Handle(new GetCategoriesQuery(), CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal(2, result.Data!.Count());
            // Danh mục mới luôn sinh ra ở trạng thái tắt (BR-04 của CreateCategoryCommand),
            // nên cả hai đều IsActive=false — điều quan trọng là chúng KHÔNG bị lọc bỏ.
            Assert.All(result.Data, c => Assert.False(c.IsActive));
        }

        // ------------------------------------------------------------ TC-CategoryService-015
        [Fact]
        [Trait("TC", "TC-UNIT-CategoryService-015")]
        [Trait("UC", "UC-78")]
        [Trait("Type", "Boundary & Negative")]
        [Trait("Priority", "Low")]
        public async Task Categories_None_EmptyList()
        {
            _categories.Setup(r => r.GetAllOrderedAsync(It.IsAny<CancellationToken>()))
                       .ReturnsAsync(new List<Category>());

            var handler = new GetCategoriesQueryHandler(_uow.Object);
            var result = await handler.Handle(new GetCategoriesQuery(), CancellationToken.None);

            Assert.True(result.Success);
            Assert.Empty(result.Data!);
        }

        // ------------------------------------------------------------ TC-CategoryService-016
        // Covers: Main Flow — bộ lọc danh mục cho khách xem danh mục khoá học.
        [Fact]
        [Trait("TC", "TC-UNIT-CategoryService-016")]
        [Trait("UC", "UC-03")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task ActiveCategories_ReturnsRepositoryResult()
        {
            var a = new CategoryBuilder().WithName("AI cơ bản").WithOrderIndex(1).Build();
            var b = new CategoryBuilder().WithName("AI nâng cao").WithOrderIndex(2).Build();

            _categories.Setup(r => r.GetActiveCategoriesAsync())
                       .ReturnsAsync(new List<Category> { a, b });

            var handler = new GetActiveCategoriesQueryHandler(_uow.Object);
            var result = await handler.Handle(new GetActiveCategoriesQuery(), CancellationToken.None);

            Assert.Equal(2, result.Count);
            Assert.Equal("AI cơ bản", result[0].Name);
            Assert.Equal(1, result[0].OrderIndex);
        }

        // ------------------------------------------------------------ TC-CategoryService-017
        // Việc lọc IsActive nằm ở repository, không ở handler. Test khẳng định handler
        // gọi ĐÚNG phương thức đã lọc chứ không phải GetAllOrderedAsync — nhầm hai hàm này
        // sẽ để lộ danh mục chưa kích hoạt ra trang công khai.
        [Fact]
        [Trait("TC", "TC-UNIT-CategoryService-017")]
        [Trait("UC", "UC-03")]
        [Trait("BR", "BR-01")]
        [Trait("Type", "Security (GBR)")]
        [Trait("Priority", "High")]
        public async Task ActiveCategories_UsesFilteredRepositoryMethod()
        {
            _categories.Setup(r => r.GetActiveCategoriesAsync()).ReturnsAsync(new List<Category>());

            var handler = new GetActiveCategoriesQueryHandler(_uow.Object);
            await handler.Handle(new GetActiveCategoriesQuery(), CancellationToken.None);

            _categories.Verify(r => r.GetActiveCategoriesAsync(), Times.Once);
            _categories.Verify(r => r.GetAllOrderedAsync(It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
