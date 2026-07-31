using AILA.Application.Common.Interfaces;
using AILA.Application.Common.Interfaces.Repositories;
using AILA.Application.Features.Onboarding.Commands.CompleteOnboarding;
using AILA.Application.Features.Profile.Commands.UpdateExpertProfile;
using AILA.Application.Features.Profile.Queries.GetExpertProfile;
using AILA.Application.Features.Profile.Queries.GetLearnerProfile;
using AILA.Application.Tests.Common.Builders;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using Moq;

namespace AILA.Application.Tests.Features.Profile
{
    /// <summary>
    /// Sheet: ProfileService · UC-05 / UC-13 / UC-16 / UC-32 · TC-UNIT-ProfileService-001 → 014.
    /// </summary>
    public class ProfileHandlerTests
    {
        private readonly Mock<IExpertRepository> _experts = new();
        private readonly Mock<ILearnerRepository> _learners = new();
        private readonly Mock<ITagRepository> _tags = new();
        private readonly Mock<IEnrollmentRepository> _enrollments = new();
        private readonly Mock<IQuizRepository> _quizzes = new();
        private readonly Mock<IUnitOfWork> _uow = new();

        public ProfileHandlerTests()
        {
            _uow.Setup(u => u.Experts).Returns(_experts.Object);
            _uow.Setup(u => u.Learners).Returns(_learners.Object);
            _uow.Setup(u => u.Tags).Returns(_tags.Object);
            _uow.Setup(u => u.Enrollments).Returns(_enrollments.Object);
            _uow.Setup(u => u.Quizzes).Returns(_quizzes.Object);
        }

        private GetExpertProfileQueryHandler GetExpertHandler() => new(_uow.Object);
        private UpdateExpertProfileCommandHandler UpdateExpertHandler() => new(_uow.Object);
        private CompleteOnboardingCommandHandler OnboardingHandler() => new(_uow.Object);
        private GetLearnerProfileQueryHandler LearnerProfileHandler() => new(_uow.Object);

        private void VerifyNotSaved()
            => _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

        // ============================================================ TC-001
        // Covers: Main Flow.
        // ⚠ Lệch UCS: UCS mô tả public profile GỒM danh sách course đã publish, nhưng
        // ExpertProfileDto KHÔNG có field nào chứa course và handler không truy vấn course.
        // Test khoá đúng những gì handler thật sự trả về (xem DEF-PRF-01).
        [Fact(Skip = "DEF-PRF-01 - Expert profile returns no published-course list")]
        [Trait("TC", "TC-UNIT-ProfileService-001")]
        [Trait("UC", "UC-05")]
        [Trait("BR", "BR-01")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        [Trait("Defect", "DEF-PRF-01")]
        public async Task GetExpertProfile_Active_NoCourseList()
        {
            var expert = new ExpertBuilder()
                .WithFullName("Dr An").WithEmail("expert@aila.com")
                .WithBio("Chuyên gia AI").WithSpecialty("AI Literacy").WithYears(5)
                .Build();
            _experts.Setup(r => r.GetReadonlyWithUserAsync(expert.UserId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(expert);

            var result = await GetExpertHandler().Handle(
                new GetExpertProfileQuery(expert.UserId), CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal("Dr An", result.Data!.FullName);
            Assert.Equal("expert@aila.com", result.Data.Email);
            Assert.Equal("Expert", result.Data.Role);
            Assert.Equal("Chuyên gia AI", result.Data.Expert.Bio);
            Assert.Equal("AI Literacy", result.Data.Expert.Specialty);
            Assert.Equal(5, result.Data.Expert.YearsOfExperience);

            // Ghim khoảng trống so với UCS: DTO không hề có chỗ chứa danh sách khoá học.
            Assert.DoesNotContain(
                typeof(AILA.Application.Features.Profile.Dtos.ExpertProfileDto).GetProperties(),
                p => p.Name.Contains("Course", StringComparison.OrdinalIgnoreCase));
        }

        // ============================================================ TC-002
        // Covers: AF-01 unavailable — hai lý do khác nhau, hai mã lỗi khác nhau.
        [Fact]
        [Trait("TC", "TC-UNIT-ProfileService-002")]
        [Trait("UC", "UC-05")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        public async Task GetExpertProfile_NotFound_Rejected()
        {
            _experts.Setup(r => r.GetReadonlyWithUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((Expert?)null);

            var result = await GetExpertHandler().Handle(
                new GetExpertProfileQuery(Guid.NewGuid()), CancellationToken.None);

            Assert.False(result.Success);
            Assert.Equal("EXPERT_NOT_FOUND", result.ErrorCode);
        }

        [Fact]
        [Trait("TC", "TC-UNIT-ProfileService-031")]
        [Trait("UC", "UC-05")]
        [Trait("Type", "Security (GBR)")]
        [Trait("Priority", "Medium")]
        public async Task GetExpertProfile_Inactive_Rejected()
        {
            var expert = new ExpertBuilder().InactiveUser().Build();
            _experts.Setup(r => r.GetReadonlyWithUserAsync(expert.UserId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(expert);

            var result = await GetExpertHandler().Handle(
                new GetExpertProfileQuery(expert.UserId), CancellationToken.None);

            Assert.Equal("ACCOUNT_INACTIVE", result.ErrorCode);
            Assert.Null(result.Data);
        }

        // ============================================================ TC-004
        // Covers: Main Flow. Workbook đoán một method updateProfile chung; code có hai handler
        // riêng cho Expert và Learner. Ở đây test nhánh Expert.
        [Fact]
        [Trait("TC", "TC-UNIT-ProfileService-004")]
        [Trait("UC", "UC-13")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task UpdateExpert_Valid_UpdatesUserAndExpert()
        {
            var expert = new ExpertBuilder().WithFullName("Cũ").WithYears(1).Build();
            _experts.Setup(r => r.GetWithUserAsync(expert.UserId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(expert);

            var result = await UpdateExpertHandler().Handle(
                new UpdateExpertProfileCommand(
                    expert.UserId, "Dr An", "https://res.cloudinary.com/x.jpg", "Bio mới", "NLP", 7),
                CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal("Dr An", expert.User.FullName);
            Assert.Equal("https://res.cloudinary.com/x.jpg", expert.User.AvatarUrl);
            Assert.Equal("Bio mới", expert.Bio);
            Assert.Equal("NLP", expert.Specialty);
            Assert.Equal(7, expert.YearsOfExperience);
            Assert.Equal("Dr An", result.Data!.FullName);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        // ============================================================ TC-005 / TC-008
        // Covers: AF-01 invalid info + BR-03 avatar phải là URL hợp lệ.
        // Cả ba nhánh validate chạy TRƯỚC khi nạp expert nên không chạm DB.
        [Theory]
        [InlineData("", null, 5)]                    // FullName rỗng
        [InlineData("   ", null, 5)]                 // FullName toàn khoảng trắng
        [InlineData("Dr An", "not-a-url", 5)]        // AvatarUrl không phải absolute URI
        [InlineData("Dr An", "/relative/path.jpg", 5)]
        [InlineData("Dr An", null, -1)]              // YearsOfExperience âm
        [Trait("TC", "TC-UNIT-ProfileService-005")]
        [Trait("UC", "UC-13")]
        [Trait("Type", "Input Validation")]
        [Trait("Priority", "High")]
        public async Task UpdateExpert_InvalidInput_RejectedNoDb(
            string fullName, string? avatarUrl, int years)
        {
            var result = await UpdateExpertHandler().Handle(
                new UpdateExpertProfileCommand(Guid.NewGuid(), fullName, avatarUrl, null, null, years),
                CancellationToken.None);

            Assert.False(result.Success);
            Assert.Equal("VALIDATION_ERROR", result.ErrorCode);
            _experts.Verify(r => r.GetWithUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
            VerifyNotSaved();
        }

        // ⚠ BR-03 nói "avatar chỉ lưu URL từ Cloudinary", nhưng code chỉ kiểm URI hợp lệ —
        // KHÔNG ràng buộc origin. URL của bất kỳ host nào cũng được chấp nhận.
        [Fact(Skip = "DEF-PRF-02 - Avatar URL origin is not restricted to Cloudinary")]
        [Trait("TC", "TC-UNIT-ProfileService-007")]
        [Trait("UC", "UC-13")]
        [Trait("BR", "BR-03")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Low")]
        [Trait("Defect", "DEF-PRF-02")]
        public async Task UpdateExpert_AnyHostAvatar_NoGuard()
        {
            var expert = new ExpertBuilder().Build();
            _experts.Setup(r => r.GetWithUserAsync(expert.UserId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(expert);

            var result = await UpdateExpertHandler().Handle(
                new UpdateExpertProfileCommand(
                    expert.UserId, "Dr An", "https://evil.example.com/x.jpg", null, null, 0),
                CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal("https://evil.example.com/x.jpg", expert.User.AvatarUrl);
        }

        // ============================================================ TC-007
        // Covers: BR-02 email immutable — command không có field Email, và User.UpdateProfile
        // chỉ đụng tới FullName + AvatarUrl. Test ghim rằng không có đường nào đổi được email.
        [Fact]
        [Trait("TC", "TC-UNIT-ProfileService-008")]
        [Trait("UC", "UC-13")]
        [Trait("BR", "BR-02")]
        [Trait("Type", "Security (GBR)")]
        [Trait("Priority", "Medium")]
        public async Task UpdateExpert_Always_KeepsEmail()
        {
            var expert = new ExpertBuilder().WithEmail("old@aila.com").Build();
            _experts.Setup(r => r.GetWithUserAsync(expert.UserId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(expert);

            var result = await UpdateExpertHandler().Handle(
                new UpdateExpertProfileCommand(expert.UserId, "Dr An", null, "bio", "AI", 3),
                CancellationToken.None);

            Assert.Equal("old@aila.com", expert.User.Email);
            Assert.Equal("old@aila.com", result.Data!.Email);
            Assert.DoesNotContain(
                typeof(UpdateExpertProfileCommand).GetProperties(),
                p => p.Name.Equals("Email", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        [Trait("TC", "TC-UNIT-ProfileService-008")]
        [Trait("UC", "UC-13")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        public async Task UpdateExpert_Inactive_RejectedNoSave()
        {
            var expert = new ExpertBuilder().InactiveUser().Build();
            _experts.Setup(r => r.GetWithUserAsync(expert.UserId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(expert);

            var result = await UpdateExpertHandler().Handle(
                new UpdateExpertProfileCommand(expert.UserId, "Dr An", null, null, null, 0),
                CancellationToken.None);

            Assert.Equal("ACCOUNT_INACTIVE", result.ErrorCode);
            VerifyNotSaved();
        }

        // ============================================================ TC-009
        // Covers: Main Flow — onboarding thành công ghi cả LearnerType, KnowledgeLevel,
        // LearningGoals và bật cờ HasCompletedOnboarding.
        [Fact]
        [Trait("TC", "TC-UNIT-ProfileService-009")]
        [Trait("UC", "UC-16")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task Onboarding_Valid_SetsGoalsAndFlag()
        {
            var learner = new LearnerBuilder().Build();
            var tag1 = Tag.CreateByAdmin("AI Cơ bản", "AI_BASIC");
            var tag2 = Tag.CreateByAdmin("Prompt", "PROMPT");
            _learners.Setup(r => r.GetWithUserAndGoalsAsync(learner.UserId, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(learner);
            _tags.Setup(r => r.GetByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new List<Tag> { tag1, tag2 });

            var result = await OnboardingHandler().Handle(
                new CompleteOnboardingCommand
                {
                    UserId = learner.UserId,
                    LearnerType = LearnerType.OfficeWorker,
                    KnowledgeLevel = KnowledgeLevel.Intermediate,
                    TagIds = new List<Guid> { tag1.Id, tag2.Id }
                },
                CancellationToken.None);

            Assert.True(result.Success);
            Assert.True(result.Data);
            Assert.True(learner.HasCompletedOnboarding);
            Assert.Equal(LearnerType.OfficeWorker, learner.LearnerType);
            Assert.Equal(KnowledgeLevel.Intermediate, learner.KnowledgeLevel);
            Assert.Equal(2, learner.LearningGoals.Count);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        [Trait("TC", "TC-UNIT-ProfileService-010")]
        [Trait("UC", "UC-16")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        public async Task Onboarding_LearnerNotFound_Rejected()
        {
            _learners.Setup(r => r.GetWithUserAndGoalsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync((Learner?)null);

            var result = await OnboardingHandler().Handle(
                new CompleteOnboardingCommand { UserId = Guid.NewGuid() }, CancellationToken.None);

            Assert.Equal("LEARNER_NOT_FOUND", result.ErrorCode);
            _tags.Verify(r => r.GetByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()), Times.Never);
            VerifyNotSaved();
        }

        // ============================================================ TC-010
        // Covers: BR-02 required selections. Handler chỉ chặn khi repo trả VỀ RỖNG.
        [Fact]
        [Trait("TC", "TC-UNIT-ProfileService-011")]
        [Trait("UC", "UC-16")]
        [Trait("BR", "BR-02")]
        [Trait("Type", "Input Validation")]
        [Trait("Priority", "High")]
        public async Task Onboarding_NoValidTag_RejectedNoSave()
        {
            var learner = new LearnerBuilder().Build();
            _learners.Setup(r => r.GetWithUserAndGoalsAsync(learner.UserId, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(learner);
            _tags.Setup(r => r.GetByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new List<Tag>());

            var result = await OnboardingHandler().Handle(
                new CompleteOnboardingCommand { UserId = learner.UserId, TagIds = new List<Guid>() },
                CancellationToken.None);

            Assert.False(result.Success);
            Assert.Equal("INVALID_TAGS", result.ErrorCode);
            Assert.False(learner.HasCompletedOnboarding);
            VerifyNotSaved();
        }

        // ⚠ Handler KHÔNG đối chiếu số lượng tag tìm được với số id gửi lên. Gửi 3 id mà chỉ
        // 1 id có thật thì vẫn đi tiếp với 1 tag — người dùng tưởng đã chọn 3 mục tiêu.
        [Fact(Skip = "DEF-PRF-03 - Onboarding silently accepts tag ids that do not exist")]
        [Trait("TC", "TC-UNIT-ProfileService-012")]
        [Trait("UC", "UC-16")]
        [Trait("BR", "BR-02")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        [Trait("Defect", "DEF-PRF-03")]
        public async Task Onboarding_MissingTagIds_SilentlyFewer()
        {
            var learner = new LearnerBuilder().Build();
            var onlyRealTag = Tag.CreateByAdmin("AI Cơ bản", "AI_BASIC");
            _learners.Setup(r => r.GetWithUserAndGoalsAsync(learner.UserId, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(learner);
            _tags.Setup(r => r.GetByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new List<Tag> { onlyRealTag });

            var result = await OnboardingHandler().Handle(
                new CompleteOnboardingCommand
                {
                    UserId = learner.UserId,
                    TagIds = new List<Guid> { onlyRealTag.Id, Guid.NewGuid(), Guid.NewGuid() }
                },
                CancellationToken.None);

            Assert.True(result.Success);
            Assert.Single(learner.LearningGoals);   // gửi 3, lưu 1, không hề báo lỗi
        }

        // ============================================================ TC-011
        // Covers: BR-03 predefined only — tag chưa được admin duyệt bị domain chặn.
        // Handler KHÔNG bắt InvalidOperationException nên nó nổi ra ngoài.
        [Fact]
        [Trait("TC", "TC-UNIT-ProfileService-012")]
        [Trait("UC", "UC-16")]
        [Trait("BR", "BR-03")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        public async Task Onboarding_UnapprovedTag_ThrowsNoSave()
        {
            var learner = new LearnerBuilder().Build();
            var draftTag = Tag.CreateByExpert("Tag nháp", "DRAFT", Guid.NewGuid());   // IsPublished=false
            _learners.Setup(r => r.GetWithUserAndGoalsAsync(learner.UserId, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(learner);
            _tags.Setup(r => r.GetByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new List<Tag> { draftTag });

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => OnboardingHandler().Handle(
                    new CompleteOnboardingCommand
                    {
                        UserId = learner.UserId,
                        TagIds = new List<Guid> { draftTag.Id }
                    },
                    CancellationToken.None));

            Assert.Contains("chưa được phê duyệt", ex.Message);
            Assert.False(learner.HasCompletedOnboarding);
            VerifyNotSaved();
        }

        // ============================================================ TC-012
        // Covers: BR-04 once only — onboarding lần hai bị domain chặn.
        [Fact]
        [Trait("TC", "TC-UNIT-ProfileService-012")]
        [Trait("UC", "UC-16")]
        [Trait("BR", "BR-04")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        public async Task Onboarding_SecondTime_ThrowsNoSave()
        {
            var learner = new LearnerBuilder().AlreadyOnboarded().Build();
            var tag = Tag.CreateByAdmin("AI Cơ bản", "AI_BASIC");
            _learners.Setup(r => r.GetWithUserAndGoalsAsync(learner.UserId, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(learner);
            _tags.Setup(r => r.GetByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new List<Tag> { tag });

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => OnboardingHandler().Handle(
                    new CompleteOnboardingCommand
                    {
                        UserId = learner.UserId,
                        TagIds = new List<Guid> { tag.Id }
                    },
                    CancellationToken.None));

            Assert.Contains("đã hoàn thành khảo sát", ex.Message);
            VerifyNotSaved();
        }

        // ============================================================ TC-013
        // Covers: Main Flow — thống kê tính trên TOÀN BỘ dữ liệu, không phải chỉ 5 mục preview.
        //
        // Phạm vi L1: chỉ khẳng định phần LOGIC THỐNG KÊ của handler. Hai danh sách preview
        // (recentEnrollments / recentQuizHistory) là IEnumerable lười, chỉ chạy mapper khi bị
        // duyệt; mapper lại cần đồ thị navigation đầy đủ (Course, Category, QuizMaterial.Material)
        // nên phần đó thuộc L2. Không cố dựng đồ thị EF trong unit test.
        [Fact]
        [Trait("TC", "TC-UNIT-ProfileService-013")]
        [Trait("UC", "UC-32")]
        [Trait("BR", "BR-01")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        public async Task LearnerProfile_WithData_SummaryOverAll()
        {
            var learner = new LearnerBuilder().WithFullName("An").Build();
            _learners.Setup(r => r.GetReadonlyWithUserAndGoalsAsync(learner.UserId, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(learner);

            // 3 enrollment: 1 hoàn thành (1/1 material), 2 đang học.
            var completed = new Enrollment(learner.UserId, Guid.NewGuid(), 1);
            completed.CompleteMaterial();
            var active1 = new Enrollment(learner.UserId, Guid.NewGuid(), 5);
            var active2 = new Enrollment(learner.UserId, Guid.NewGuid(), 5);
            _enrollments.Setup(r => r.GetEnrollmentsWithCourseByLearnerIdAsync(
                            learner.UserId, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new List<Enrollment> { completed, active1, active2 });

            // 3 attempt: điểm 80 (đạt), 40 (trượt), 71 (đạt) → trung bình 63.67 sau làm tròn 2 chữ số.
            var attempts = new List<QuizAttempt>
            {
                MakeAttempt(80m, true), MakeAttempt(40m, false), MakeAttempt(71m, true)
            };
            _quizzes.Setup(r => r.GetSubmittedAttemptsByLearnerAsync(learner.UserId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(attempts);

            var result = await LearnerProfileHandler().Handle(
                new GetLearnerProfileQuery(learner.UserId), CancellationToken.None);

            Assert.True(result.Success);
            var s = result.Data!.Summary;
            Assert.Equal(3, s.TotalCourses);
            Assert.Equal(2, s.CoursesInProgress);
            Assert.Equal(1, s.CoursesCompleted);
            Assert.Equal(3, s.TotalQuizzesTaken);
            Assert.Equal(2, s.QuizzesPassed);
            Assert.Equal(63.67m, s.AverageQuizScore);
        }

        private static QuizAttempt MakeAttempt(decimal score, bool passed)
        {
            var attempt = new QuizAttempt(Guid.NewGuid(), Guid.NewGuid());
            attempt.Submit(score, passed);
            return attempt;
        }

        // ============================================================ TC-014
        // Covers: AF-01 empty — learner chưa học gì thì mọi chỉ số = 0 và
        // AverageQuizScore = null (tránh chia cho 0), KHÔNG phải lỗi.
        [Fact]
        [Trait("TC", "TC-UNIT-ProfileService-014")]
        [Trait("UC", "UC-32")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Low")]
        public async Task LearnerProfile_NoData_ZeroedSummary()
        {
            var learner = new LearnerBuilder().Build();
            _learners.Setup(r => r.GetReadonlyWithUserAndGoalsAsync(learner.UserId, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(learner);
            _enrollments.Setup(r => r.GetEnrollmentsWithCourseByLearnerIdAsync(
                            learner.UserId, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new List<Enrollment>());
            _quizzes.Setup(r => r.GetSubmittedAttemptsByLearnerAsync(learner.UserId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<QuizAttempt>());

            var result = await LearnerProfileHandler().Handle(
                new GetLearnerProfileQuery(learner.UserId), CancellationToken.None);

            Assert.True(result.Success);
            var s = result.Data!.Summary;
            Assert.Equal(0, s.TotalCourses);
            Assert.Equal(0, s.TotalQuizzesTaken);
            Assert.Null(s.AverageQuizScore);
            Assert.Empty(result.Data.Enrollments);
            Assert.Empty(result.Data.QuizHistory);
            Assert.Empty(result.Data.AiScenarioHistory);
        }

        [Fact]
        [Trait("TC", "TC-UNIT-ProfileService-014")]
        [Trait("UC", "UC-32")]
        [Trait("Type", "Security (GBR)")]
        [Trait("Priority", "Medium")]
        public async Task LearnerProfile_Inactive_RejectedNoDb()
        {
            var learner = new LearnerBuilder().InactiveUser().Build();
            _learners.Setup(r => r.GetReadonlyWithUserAndGoalsAsync(learner.UserId, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(learner);

            var result = await LearnerProfileHandler().Handle(
                new GetLearnerProfileQuery(learner.UserId), CancellationToken.None);

            Assert.Equal("ACCOUNT_INACTIVE", result.ErrorCode);
            _enrollments.Verify(r => r.GetEnrollmentsWithCourseByLearnerIdAsync(
                It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
