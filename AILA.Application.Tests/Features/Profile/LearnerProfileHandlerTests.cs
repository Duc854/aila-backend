using AILA.Application.Common.Interfaces;
using AILA.Application.Common.Interfaces.Repositories;
using AILA.Application.Features.Experts.Queries;
using AILA.Application.Features.Onboarding.Queries.GetOnboardingStatus;
using AILA.Application.Features.Profile.Commands.UpdateLearnerProfile;
using AILA.Application.Features.Profile.Commands.UploadAvatar;
using AILA.Application.Features.Profile.Queries.GetLearnerAiScenarios;
using AILA.Application.Features.Profile.Queries.GetLearnerCourses;
using AILA.Application.Features.Profile.Queries.GetLearnerQuizHistory;
using AILA.Application.Tests.Common;
using AILA.Application.Tests.Common.Builders;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using Moq;
using Shared.Wrappers;

namespace AILA.Application.Tests.Features.Profile
{
    /// <summary>
    /// Sheet: ProfileService · UC-13 Update profile / UC-16 Onboarding status /
    /// UC-32 Learning profile / UC-05 Public expert page.
    /// TC-UNIT-ProfileService-015 → 031.
    /// </summary>
    public class LearnerProfileHandlerTests
    {
        private readonly Mock<ILearnerRepository> _learners = new();
        private readonly Mock<IUserRepository> _users = new();
        private readonly Mock<ITagRepository> _tags = new();
        private readonly Mock<IExpertRepository> _experts = new();
        private readonly Mock<ICourseRepository> _courses = new();
        private readonly Mock<IEnrollmentRepository> _enrollments = new();
        private readonly Mock<IQuizRepository> _quizzes = new();
        private readonly Mock<IFileStorageService> _storage = new();
        private readonly Mock<IUnitOfWork> _uow = new();

        public LearnerProfileHandlerTests()
        {
            _uow.Setup(u => u.Learners).Returns(_learners.Object);
            _uow.Setup(u => u.Users).Returns(_users.Object);
            _uow.Setup(u => u.Tags).Returns(_tags.Object);
            _uow.Setup(u => u.Experts).Returns(_experts.Object);
            _uow.Setup(u => u.Courses).Returns(_courses.Object);
            _uow.Setup(u => u.Enrollments).Returns(_enrollments.Object);
            _uow.Setup(u => u.Quizzes).Returns(_quizzes.Object);
        }

        private static Tag PublishedTag(string code = "goal-1")
        {
            var tag = Tag.CreateByAdmin("Mục tiêu", code);   // tag do admin tạo là đã published
            return tag;
        }

        private static UpdateLearnerProfileCommand UpdateCmd(
            Guid userId,
            string fullName = "Nguyen An",
            string? avatarUrl = null,
            Guid[]? tagIds = null)
            => new(userId, fullName, avatarUrl, LearnerType.Student, KnowledgeLevel.Beginner,
                   tagIds ?? new[] { Guid.NewGuid() });

        // ============================================================ UC-13 UpdateLearnerProfile

        // ------------------------------------------------------------ TC-015
        [Fact]
        [Trait("TC", "TC-UNIT-ProfileService-015")]
        [Trait("UC", "UC-13")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task UpdateLearner_Valid_SavesProfileAndGoals()
        {
            var learner = new LearnerBuilder().WithFullName("Ten cu").Build();
            var tag = PublishedTag();

            _learners.Setup(r => r.GetWithUserAndGoalsAsync(learner.UserId, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(learner);
            _tags.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new List<Tag> { tag });

            var handler = new UpdateLearnerProfileCommandHandler(_uow.Object);
            var result = await handler.Handle(
                UpdateCmd(learner.UserId, "Nguyen An", tagIds: new[] { tag.Id }), CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal("Nguyen An", learner.User.FullName);
            _learners.Verify(r => r.SetLearnerDetails(learner, LearnerType.Student, KnowledgeLevel.Beginner),
                Times.Once);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        // ------------------------------------------------------------ TC-016
        // Covers: BR-01 — ba nhánh validate chạy TRƯỚC khi chạm database, nên input sai
        // không tốn một truy vấn nào.
        [Fact]
        [Trait("TC", "TC-UNIT-ProfileService-016")]
        [Trait("UC", "UC-13")]
        [Trait("BR", "BR-01")]
        [Trait("Type", "Input Validation")]
        [Trait("Priority", "High")]
        public async Task UpdateLearner_InvalidInput_FailsBeforeLookup()
        {
            var handler = new UpdateLearnerProfileCommandHandler(_uow.Object);
            var userId = Guid.NewGuid();

            // (a) tên rỗng
            var blank = await handler.Handle(UpdateCmd(userId, fullName: "  "), CancellationToken.None);
            Assert.Equal("VALIDATION_ERROR", blank.ErrorCode);

            // (b) avatar không phải URL tuyệt đối
            var badUrl = await handler.Handle(
                UpdateCmd(userId, avatarUrl: "khong-phai-url"), CancellationToken.None);
            Assert.Equal("VALIDATION_ERROR", badUrl.ErrorCode);

            // (c) danh sách mục tiêu rỗng
            var noGoal = await handler.Handle(
                UpdateCmd(userId, tagIds: Array.Empty<Guid>()), CancellationToken.None);
            Assert.Equal("VALIDATION_ERROR", noGoal.ErrorCode);

            _learners.Verify(r => r.GetWithUserAndGoalsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        // ------------------------------------------------------------ TC-017
        // Covers: AF-01 — không có hồ sơ học viên, hoặc tài khoản đã bị admin khoá (UC-72).
        [Fact]
        [Trait("TC", "TC-UNIT-ProfileService-017")]
        [Trait("UC", "UC-13")]
        [Trait("Type", "Security (GBR)")]
        [Trait("Priority", "High")]
        public async Task UpdateLearner_MissingOrInactive_Rejected()
        {
            var handler = new UpdateLearnerProfileCommandHandler(_uow.Object);

            _learners.Setup(r => r.GetWithUserAndGoalsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync((Learner?)null);
            var missing = await handler.Handle(UpdateCmd(Guid.NewGuid()), CancellationToken.None);
            Assert.Equal("LEARNER_NOT_FOUND", missing.ErrorCode);

            var locked = new LearnerBuilder().InactiveUser().Build();
            _learners.Setup(r => r.GetWithUserAndGoalsAsync(locked.UserId, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(locked);
            var inactive = await handler.Handle(UpdateCmd(locked.UserId), CancellationToken.None);
            Assert.Equal("ACCOUNT_INACTIVE", inactive.ErrorCode);

            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        // ------------------------------------------------------------ TC-018
        // Covers: BR-02 — mục tiêu phải là tag CÓ THẬT và ĐÃ DUYỆT. Nếu không, learner có thể
        // gắn tag nháp riêng của một expert khác vào hồ sơ mình.
        [Fact]
        [Trait("TC", "TC-UNIT-ProfileService-017")]
        [Trait("UC", "UC-13")]
        [Trait("BR", "BR-02")]
        [Trait("Type", "Security (GBR)")]
        [Trait("Priority", "High")]
        public async Task UpdateLearner_UnknownOrDraftTag_Rejected()
        {
            var learner = new LearnerBuilder().Build();
            _learners.Setup(r => r.GetWithUserAndGoalsAsync(learner.UserId, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(learner);

            var handler = new UpdateLearnerProfileCommandHandler(_uow.Object);

            // (a) yêu cầu 2 tag nhưng chỉ tra được 1 -> có id không tồn tại
            _tags.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new List<Tag> { PublishedTag() });
            var unknown = await handler.Handle(
                UpdateCmd(learner.UserId, tagIds: new[] { Guid.NewGuid(), Guid.NewGuid() }),
                CancellationToken.None);
            Assert.Equal("TAG_NOT_FOUND", unknown.ErrorCode);

            // (b) tag tồn tại nhưng là tag nháp của expert, chưa được duyệt
            var draft = Tag.CreateByExpert("Nháp", "draft-tag", Guid.NewGuid());
            _tags.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new List<Tag> { draft });
            var unpublished = await handler.Handle(
                UpdateCmd(learner.UserId, tagIds: new[] { draft.Id }), CancellationToken.None);
            Assert.Equal("UNPUBLISHED_TAG", unpublished.ErrorCode);

            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        // ============================================================ UC-13 UploadAvatar

        // ------------------------------------------------------------ TC-019
        [Fact]
        [Trait("TC", "TC-UNIT-ProfileService-018")]
        [Trait("UC", "UC-13")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task UploadAvatar_ValidImage_StoresUrl()
        {
            var user = new UserBuilder().Build();
            _users.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);
            _storage.Setup(s => s.UploadImageAsync(
                        It.IsAny<Stream>(), "av.png", "avatars", It.IsAny<CancellationToken>()))
                    .ReturnsAsync("https://cdn/av.png");

            var handler = new UploadAvatarCommandHandler(_uow.Object, _storage.Object);
            var result = await handler.Handle(
                new UploadAvatarCommand(user.Id, Stream.Null, "av.png", "image/png", 1024),
                CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal("https://cdn/av.png", result.Data!.AvatarUrl);
            Assert.Equal("https://cdn/av.png", user.AvatarUrl);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        // ------------------------------------------------------------ TC-020
        // Covers: BR-01 biên dung lượng 5 MB và whitelist định dạng. Mọi nhánh phải chặn
        // TRƯỚC khi một byte nào rời máy chủ đi lên nhà cung cấp lưu trữ.
        [Theory]
        [InlineData(0, "image/png")]                       // rỗng
        [InlineData(5 * 1024 * 1024 + 1, "image/png")]     // vượt 5 MB một byte
        [InlineData(1024, "application/x-msdownload")]     // .exe
        [InlineData(1024, "image/gif")]                    // ngoài whitelist jpeg/png/webp
        [Trait("TC", "TC-UNIT-ProfileService-019")]
        [Trait("UC", "UC-13")]
        [Trait("BR", "BR-01")]
        [Trait("Type", "Boundary & Negative")]
        [Trait("Priority", "High")]
        public async Task UploadAvatar_InvalidFile_RejectedBeforeUpload(long size, string contentType)
        {
            var handler = new UploadAvatarCommandHandler(_uow.Object, _storage.Object);
            var result = await handler.Handle(
                new UploadAvatarCommand(Guid.NewGuid(), Stream.Null, "f", contentType, size),
                CancellationToken.None);

            Assert.False(result.Success);
            Assert.Equal("VALIDATION_ERROR", result.ErrorCode);
            _storage.Verify(s => s.UploadImageAsync(
                It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
            _users.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        }

        // ------------------------------------------------------------ TC-021
        // Covers: BR-02 — đúng biên 5 MB (không dư một byte) vẫn phải được chấp nhận.
        [Fact]
        [Trait("TC", "TC-UNIT-ProfileService-020")]
        [Trait("UC", "UC-13")]
        [Trait("BR", "BR-02")]
        [Trait("Type", "Boundary & Negative")]
        [Trait("Priority", "Medium")]
        public async Task UploadAvatar_ExactlyFiveMegabytes_Accepted()
        {
            var user = new UserBuilder().Build();
            _users.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);
            _storage.Setup(s => s.UploadImageAsync(
                        It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync("https://cdn/big.jpg");

            var handler = new UploadAvatarCommandHandler(_uow.Object, _storage.Object);
            var result = await handler.Handle(
                new UploadAvatarCommand(user.Id, Stream.Null, "big.jpg", "image/jpeg", 5 * 1024 * 1024),
                CancellationToken.None);

            Assert.True(result.Success);
        }

        // ------------------------------------------------------------ TC-022
        // Covers: AF-01 — người dùng không tồn tại hoặc bị khoá thì KHÔNG được upload.
        [Fact]
        [Trait("TC", "TC-UNIT-ProfileService-020")]
        [Trait("UC", "UC-13")]
        [Trait("Type", "Security (GBR)")]
        [Trait("Priority", "High")]
        public async Task UploadAvatar_MissingOrInactiveUser_NoUpload()
        {
            var handler = new UploadAvatarCommandHandler(_uow.Object, _storage.Object);

            _users.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);
            var missing = await handler.Handle(
                new UploadAvatarCommand(Guid.NewGuid(), Stream.Null, "a.png", "image/png", 1024),
                CancellationToken.None);
            Assert.Equal("USER_NOT_FOUND", missing.ErrorCode);

            var locked = new UserBuilder().Inactive().Build();
            _users.Setup(r => r.GetByIdAsync(locked.Id)).ReturnsAsync(locked);
            var inactive = await handler.Handle(
                new UploadAvatarCommand(locked.Id, Stream.Null, "a.png", "image/png", 1024),
                CancellationToken.None);
            Assert.Equal("ACCOUNT_INACTIVE", inactive.ErrorCode);

            _storage.Verify(s => s.UploadImageAsync(
                It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        // ============================================================ UC-16 Onboarding status

        // ------------------------------------------------------------ TC-023
        [Fact]
        [Trait("TC", "TC-UNIT-ProfileService-027")]
        [Trait("UC", "UC-16")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        public async Task OnboardingStatus_Completed_ReportsDetails()
        {
            var learner = new LearnerBuilder().AlreadyOnboarded().Build();
            _learners.Setup(r => r.GetReadonlyWithUserAndGoalsAsync(learner.UserId, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(learner);

            var handler = new GetOnboardingStatusQueryHandler(_uow.Object);
            var result = await handler.Handle(
                new GetOnboardingStatusQuery { UserId = learner.UserId }, CancellationToken.None);

            Assert.True(result.Success);
            Assert.True(result.Data!.HasCompletedOnboarding);
            Assert.Equal(nameof(LearnerType.Student), result.Data.LearnerType);
            Assert.Equal(nameof(KnowledgeLevel.Beginner), result.Data.KnowledgeLevel);
        }

        // ------------------------------------------------------------ TC-024
        [Fact]
        [Trait("TC", "TC-UNIT-ProfileService-028")]
        [Trait("UC", "UC-16")]
        [Trait("Type", "Boundary & Negative")]
        [Trait("Priority", "Medium")]
        public async Task OnboardingStatus_NoLearnerRow_NotFound()
        {
            _learners.Setup(r => r.GetReadonlyWithUserAndGoalsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync((Learner?)null);

            var handler = new GetOnboardingStatusQueryHandler(_uow.Object);
            var result = await handler.Handle(
                new GetOnboardingStatusQuery { UserId = Guid.NewGuid() }, CancellationToken.None);

            Assert.False(result.Success);
            Assert.Equal("LEARNER_NOT_FOUND", result.ErrorCode);
        }

        // ============================================================ UC-32 Learning profile

        // ------------------------------------------------------------ TC-025
        // Covers: Main Flow — khoá học đã ghi danh, kèm tiến độ.
        [Fact]
        [Trait("TC", "TC-UNIT-ProfileService-021")]
        [Trait("UC", "UC-32")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        public async Task LearnerCourses_MapsEnrollmentWithProgress()
        {
            var course = new CourseBuilder().WithName("AI nhập môn").Build();
            var enrollment = new Enrollment(Guid.NewGuid(), course.Id, 10);
            TestEntity.SetProperty(enrollment, nameof(Enrollment.Course), course);

            _enrollments.Setup(r => r.GetPagedEnrollmentsByLearnerAsync(
                        It.IsAny<Guid>(), 1, 10, It.IsAny<CancellationToken>()))
                        .ReturnsAsync((new List<Enrollment> { enrollment }, 1));

            var handler = new GetLearnerCoursesQueryHandler(_uow.Object);
            var result = await handler.Handle(
                new GetLearnerCoursesQuery(Guid.NewGuid(), new PageRequest { PageIndex = 1, PageSize = 10 }),
                CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal(1, result.Data!.TotalItems);
            Assert.Equal("AI nhập môn", result.Data.Items.First().CourseName);
            Assert.Equal(10, result.Data.Items.First().TotalMaterials);
        }

        // ------------------------------------------------------------ TC-026
        // Covers: BR-01 — PagingDefaults.Normalize kẹp tham số phân trang trước khi
        // xuống repository: PageIndex < 1 -> 1, PageSize < 1 -> 10, PageSize > 50 -> 50.
        // Không kẹp thì client gửi PageSize=100000 sẽ kéo sập truy vấn.
        [Theory]
        [InlineData(0, 10, 1, 10)]
        [InlineData(-5, 10, 1, 10)]
        [InlineData(2, 0, 2, 10)]
        [InlineData(2, 999, 2, 50)]
        [InlineData(3, 25, 3, 25)]
        [Trait("TC", "TC-UNIT-ProfileService-022")]
        [Trait("UC", "UC-32")]
        [Trait("BR", "BR-01")]
        [Trait("Type", "Boundary & Negative")]
        [Trait("Priority", "High")]
        public async Task LearnerCourses_NormalisesPaging(
            int pageIndex, int pageSize, int expectedIndex, int expectedSize)
        {
            _enrollments.Setup(r => r.GetPagedEnrollmentsByLearnerAsync(
                        It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync((new List<Enrollment>(), 0));

            var handler = new GetLearnerCoursesQueryHandler(_uow.Object);
            var learnerId = Guid.NewGuid();
            await handler.Handle(
                new GetLearnerCoursesQuery(learnerId, new PageRequest { PageIndex = pageIndex, PageSize = pageSize }),
                CancellationToken.None);

            _enrollments.Verify(r => r.GetPagedEnrollmentsByLearnerAsync(
                learnerId, expectedIndex, expectedSize, It.IsAny<CancellationToken>()), Times.Once);
        }

        // ------------------------------------------------------------ TC-027
        [Fact]
        [Trait("TC", "TC-UNIT-ProfileService-022")]
        [Trait("UC", "UC-32")]
        [Trait("Type", "Boundary & Negative")]
        [Trait("Priority", "Medium")]
        public async Task LearnerCourses_NoEnrollment_EmptyPage()
        {
            _enrollments.Setup(r => r.GetPagedEnrollmentsByLearnerAsync(
                        It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync((new List<Enrollment>(), 0));

            var handler = new GetLearnerCoursesQueryHandler(_uow.Object);
            var result = await handler.Handle(
                new GetLearnerCoursesQuery(Guid.NewGuid(), new PageRequest { PageIndex = 1, PageSize = 10 }),
                CancellationToken.None);

            Assert.True(result.Success);
            Assert.Empty(result.Data!.Items);
            Assert.Equal(0, result.Data.TotalItems);
        }

        // ------------------------------------------------------------ TC-028
        // TotalItems lấy từ repository chứ không phải Items.Count — nếu nhầm, tổng số trang sai.
        [Fact]
        [Trait("TC", "TC-UNIT-ProfileService-023")]
        [Trait("UC", "UC-32")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        public async Task QuizHistory_TotalComesFromRepository()
        {
            _quizzes.Setup(r => r.GetPagedSubmittedAttemptsByLearnerAsync(
                        It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((new List<QuizAttempt>(), 42));

            var handler = new GetLearnerQuizHistoryQueryHandler(_uow.Object);
            var result = await handler.Handle(
                new GetLearnerQuizHistoryQuery(Guid.NewGuid(), new PageRequest { PageIndex = 1, PageSize = 10 }),
                CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal(42, result.Data!.TotalItems);
            Assert.Empty(result.Data.Items);
        }

        // ------------------------------------------------------------ TC-029
        [Theory]
        [InlineData(0, 999, 1, 50)]
        [InlineData(4, 20, 4, 20)]
        [Trait("TC", "TC-UNIT-ProfileService-024")]
        [Trait("UC", "UC-32")]
        [Trait("BR", "BR-01")]
        [Trait("Type", "Boundary & Negative")]
        [Trait("Priority", "Medium")]
        public async Task QuizHistory_NormalisesPaging(
            int pageIndex, int pageSize, int expectedIndex, int expectedSize)
        {
            _quizzes.Setup(r => r.GetPagedSubmittedAttemptsByLearnerAsync(
                        It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((new List<QuizAttempt>(), 0));

            var handler = new GetLearnerQuizHistoryQueryHandler(_uow.Object);
            var learnerId = Guid.NewGuid();
            await handler.Handle(
                new GetLearnerQuizHistoryQuery(learnerId, new PageRequest { PageIndex = pageIndex, PageSize = pageSize }),
                CancellationToken.None);

            _quizzes.Verify(r => r.GetPagedSubmittedAttemptsByLearnerAsync(
                learnerId, expectedIndex, expectedSize, It.IsAny<CancellationToken>()), Times.Once);
        }

        // ------------------------------------------------------------ TC-030
        // Covers: Main Flow — endpoint tồn tại và trả lời, nhưng LUÔN rỗng: handler không
        // truy vấn gì cả vì chưa có bảng nào lưu phiên AI Practice (UC-27 chưa cài đặt).
        // Test khoá lại điều đó để khi UC-27 xong, ai đó phải sửa cả handler này.
        [Fact]
        [Trait("TC", "TC-UNIT-ProfileService-025")]
        [Trait("UC", "UC-32")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Low")]
        public async Task AiScenarios_AlwaysEmptyUntilUc27Exists()
        {
            var handler = new GetLearnerAiScenariosQueryHandler();
            var result = await handler.Handle(
                new GetLearnerAiScenariosQuery(Guid.NewGuid(), new PageRequest { PageIndex = 3, PageSize = 999 }),
                CancellationToken.None);

            Assert.True(result.Success);
            Assert.Empty(result.Data!.Items);
            Assert.Equal(0, result.Data.TotalItems);
            Assert.Equal(3, result.Data.PageNumber);
            Assert.Equal(50, result.Data.PageSize);      // vẫn bị kẹp về MaxPageSize
        }

        // ============================================================ UC-05 Public expert page

        // ------------------------------------------------------------ TC-031
        // Covers: BR-01 — trang công khai chỉ dựng từ GetPublishedByExpertAsync, và
        // chuyên gia bị khoá tài khoản thì biến mất hoàn toàn (trả null, không phải hồ sơ rỗng).
        [Fact]
        [Trait("TC", "TC-UNIT-ProfileService-029")]
        [Trait("UC", "UC-05")]
        [Trait("BR", "BR-01")]
        [Trait("Type", "Security (GBR)")]
        [Trait("Priority", "High")]
        public async Task PublicExpert_OnlyPublishedCourses_AndHiddenWhenInactive()
        {
            var expert = new ExpertBuilder().WithFullName("Tran Chuyen Gia").Build();
            var published = new CourseBuilder().OwnedBy(expert.UserId).Published().Build();

            _experts.Setup(r => r.GetReadonlyWithUserAsync(expert.UserId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(expert);
            _courses.Setup(r => r.GetPublishedByExpertAsync(expert.UserId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<Course> { published });

            var handler = new GetPublicExpertProfileQueryHandler(_uow.Object);
            var page = await handler.Handle(
                new GetPublicExpertProfileQuery(expert.UserId), CancellationToken.None);

            Assert.NotNull(page);
            Assert.Equal("Tran Chuyen Gia", page!.FullName);
            Assert.Equal(1, page.TotalPublishedCourses);

            // Chuyên gia bị khoá -> không còn trang công khai
            var locked = new ExpertBuilder().InactiveUser().Build();
            _experts.Setup(r => r.GetReadonlyWithUserAsync(locked.UserId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(locked);
            Assert.Null(await handler.Handle(
                new GetPublicExpertProfileQuery(locked.UserId), CancellationToken.None));

            // Không tồn tại -> null
            _experts.Setup(r => r.GetReadonlyWithUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((Expert?)null);
            Assert.Null(await handler.Handle(
                new GetPublicExpertProfileQuery(Guid.NewGuid()), CancellationToken.None));
        }
    }
}
