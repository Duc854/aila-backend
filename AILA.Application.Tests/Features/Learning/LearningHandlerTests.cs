using AILA.Application.Common.Interfaces;
using AILA.Application.Common.Interfaces.Repositories;
using AILA.Application.Features.Courses.Commands;
using AILA.Application.Features.Courses.Queries.GetCourseLearningView;
using AILA.Application.Features.Materials.Commands.MarkMaterialAsCompleted;
using AILA.Application.Features.Materials.Queries.GetMaterialDetail;
using AILA.Domain.Entities;
using Moq;

namespace AILA.Application.Tests.Features.Learning
{
    /// <summary>
    /// Sheet: LearningService · UC-22 / UC-23 / UC-24 · TC-UNIT-LearningService-001 → 009.
    ///
    /// Ba handler nằm rải ở nhiều feature folder khác nhau (Courses, Materials) — workbook gom
    /// chúng vào một "LearningService" theo góc nhìn nghiệp vụ.
    /// </summary>
    public class LearningHandlerTests
    {
        private readonly Mock<ICourseRepository> _courses = new();
        private readonly Mock<IEnrollmentRepository> _enrollments = new();
        private readonly Mock<IMaterialRepository> _materials = new();
        private readonly Mock<ILearningProgressRepository> _progresses = new();
        private readonly Mock<IGenericRepository<Learner>> _learners = new();
        private readonly Mock<IUnitOfWork> _uow = new();

        public LearningHandlerTests()
        {
            _uow.Setup(u => u.Courses).Returns(_courses.Object);
            _uow.Setup(u => u.Enrollments).Returns(_enrollments.Object);
            _uow.Setup(u => u.Materials).Returns(_materials.Object);
            _uow.Setup(u => u.LearningProgresses).Returns(_progresses.Object);
            _uow.Setup(u => u.Repository<Learner>()).Returns(_learners.Object);
        }

        private EnrollCourseCommandHandler EnrollHandler() => new(_uow.Object);
        private GetMaterialDetailQueryHandler MaterialDetailHandler() => new(_uow.Object);
        private MarkMaterialAsCompletedCommandHandler MarkCompletedHandler() => new(_uow.Object);
        private GetCourseLearningViewQueryHandler LearningViewHandler() => new(_uow.Object);

        private void VerifyNotSaved()
            => _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

        // ============================================================ TC-001
        // Covers: Main Flow — tạo Enrollment với tổng số học liệu đếm được tại thời điểm ghi danh.
        //
        // ⚠ Handler này báo lỗi bằng NÉM InvalidOperationException, KHÔNG dùng ResponseDto như
        // phần lớn handler khác (xem DEF-LRN-01). Test phải theo đúng cơ chế đó.
        [Fact]
        [Trait("TC", "TC-UNIT-LearningService-001")]
        [Trait("UC", "UC-22")]
        [Trait("BR", "BR-02")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Critical")]
        public async Task Enroll_Valid_CreatesWithMaterialCount()
        {
            var learnerId = Guid.NewGuid();
            var course = CourseStub(published: true);
            _courses.Setup(r => r.GetByIdAsync(course.Id)).ReturnsAsync(course);
            _learners.Setup(r => r.GetByIdAsync(learnerId)).ReturnsAsync(new Learner(learnerId));
            _enrollments.Setup(r => r.GetByLearnerAndCourseAsync(learnerId, course.Id))
                        .ReturnsAsync((Enrollment?)null);
            _courses.Setup(r => r.CountMaterialsAsync(course.Id)).ReturnsAsync(12);

            Enrollment? added = null;
            _enrollments.Setup(r => r.AddAsync(It.IsAny<Enrollment>()))
                        .Callback<Enrollment>(e => added = e)
                        .Returns(Task.CompletedTask);

            var result = await EnrollHandler().Handle(
                new EnrollCourseCommand(course.Id, learnerId), CancellationToken.None);

            Assert.NotNull(added);
            Assert.Equal(learnerId, added!.LearnerId);
            Assert.Equal(course.Id, added.CourseId);
            Assert.Equal(12, added.TotalMaterials);
            Assert.Equal(0, added.CompletedMaterials);
            Assert.Equal(added.Id, result.EnrollmentId);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        // ------------------------------------------------------------ TC-001 (nhánh chặn)
        // Ba lý do từ chối đều ném exception với thông điệp riêng.
        [Fact]
        [Trait("TC", "TC-UNIT-LearningService-002")]
        [Trait("UC", "UC-22")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task Enroll_CourseNotFound_ThrowsAndSavesNothing()
        {
            _courses.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Course?)null);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => EnrollHandler().Handle(
                    new EnrollCourseCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None));

            Assert.Contains("không tồn tại", ex.Message);
            VerifyNotSaved();
        }

        // BR-02: chỉ khoá học đã công khai mới ghi danh được.
        [Fact]
        [Trait("TC", "TC-UNIT-LearningService-002")]
        [Trait("UC", "UC-22")]
        [Trait("BR", "BR-02")]
        [Trait("Type", "Security (GBR)")]
        [Trait("Priority", "High")]
        public async Task Enroll_UnpublishedCourse_ThrowsNoSave()
        {
            var course = CourseStub(published: false);
            _courses.Setup(r => r.GetByIdAsync(course.Id)).ReturnsAsync(course);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => EnrollHandler().Handle(
                    new EnrollCourseCommand(course.Id, Guid.NewGuid()), CancellationToken.None));

            Assert.Contains("chưa được công khai", ex.Message);
            _enrollments.Verify(r => r.AddAsync(It.IsAny<Enrollment>()), Times.Never);
            VerifyNotSaved();
        }

        // ============================================================ TC-002
        // Covers: BR-01 enroll once — ghi danh lần hai bị chặn, không tạo bản ghi trùng.
        [Fact]
        [Trait("TC", "TC-UNIT-LearningService-002")]
        [Trait("UC", "UC-22")]
        [Trait("BR", "BR-01")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task Enroll_AlreadyEnrolled_ThrowsNoDupe()
        {
            var learnerId = Guid.NewGuid();
            var course = CourseStub(published: true);
            _courses.Setup(r => r.GetByIdAsync(course.Id)).ReturnsAsync(course);
            _learners.Setup(r => r.GetByIdAsync(learnerId)).ReturnsAsync(new Learner(learnerId));
            _enrollments.Setup(r => r.GetByLearnerAndCourseAsync(learnerId, course.Id))
                        .ReturnsAsync(new Enrollment(learnerId, course.Id, 3));

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => EnrollHandler().Handle(
                    new EnrollCourseCommand(course.Id, learnerId), CancellationToken.None));

            Assert.Contains("đã tham gia", ex.Message);
            _enrollments.Verify(r => r.AddAsync(It.IsAny<Enrollment>()), Times.Never);
            VerifyNotSaved();
        }

        // ============================================================ TC-005 (phần course null)
        // Covers: AF-01. Phần "course có nhưng 0 material" cần dựng đồ thị Course+Module+Material
        // → hoãn sang batch 4 (CourseService) khi đã có CourseBuilder dùng chung.
        [Fact]
        [Trait("TC", "TC-UNIT-LearningService-003")]
        [Trait("UC", "UC-23")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        public async Task LearningView_NotFound_CourseNotFound()
        {
            _courses.Setup(r => r.GetCourseWithFullContentAsync(It.IsAny<Guid>()))
                    .ReturnsAsync((Course?)null);

            var result = await LearningViewHandler().Handle(
                new GetCourseLearningViewQuery(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

            Assert.False(result.Success);
            Assert.Equal("COURSE_NOT_FOUND", result.ErrorCode);
            _progresses.Verify(r => r.GetCompletedMaterialIdsAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
        }

        // ============================================================ TC-006
        // Covers: Main Flow — map material sang DTO theo đúng loại.
        //
        // ⚠ Handler KHÔNG kiểm tra enrollment: bất kỳ ai biết courseId + materialId đều đọc
        // được nội dung bài học mà không cần đã ghi danh (xem DEF-LRN-02).
        [Fact(Skip = "DEF-LRN-02 - Material detail does not check enrolment")]
        [Trait("TC", "TC-UNIT-LearningService-006")]
        [Trait("UC", "UC-24")]
        [Trait("BR", "BR-01")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        [Trait("Defect", "DEF-LRN-02")]
        public async Task MaterialDetail_Exists_NoEnrollCheck()
        {
            var courseId = Guid.NewGuid();
            // Material.OrderIndex bắt buộc > 0 (khác Category vốn cho phép 0) — xem ghi chú
            // về convention OrderIndex không đồng nhất giữa các entity.
            var material = Material.CreateDocument(Guid.NewGuid(), "Bài 1 - AI là gì", 1);
            _materials.Setup(r => r.GetMaterialDetailAsync(courseId, material.Id, It.IsAny<CancellationToken>())).ReturnsAsync(material);

            var result = await MaterialDetailHandler().Handle(
                new GetMaterialDetailQuery(courseId, material.Id), CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal(material.Id, result.Data!.Id);
            Assert.Equal("Bài 1 - AI là gì", result.Data.Title);
            Assert.Equal("Document", result.Data.Type);

            // Không hề có lời gọi nào kiểm tra người dùng đã ghi danh hay chưa.
            _enrollments.Verify(r => r.GetByCourseAndLearnerAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        // ============================================================ TC-007
        // Covers: AF-01 unavailable. Repo lọc theo cả courseId nên material của khoá khác
        // cũng rơi vào nhánh này — không tiết lộ sự tồn tại của học liệu ngoài khoá.
        [Fact]
        [Trait("TC", "TC-UNIT-LearningService-007")]
        [Trait("UC", "UC-24")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        public async Task MaterialDetail_NotInCourse_NotFound()
        {
            _materials.Setup(r => r.GetMaterialDetailAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync((Material?)null);

            var result = await MaterialDetailHandler().Handle(
                new GetMaterialDetailQuery(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

            Assert.False(result.Success);
            Assert.Equal("MATERIAL_NOT_FOUND", result.ErrorCode);
        }

        // ============================================================ TC-008
        // Covers: BR-02 progress — lần đầu hoàn thành thì tạo LearningProgress, cộng tiến độ
        // enrollment, và COMMIT transaction.
        [Fact]
        [Trait("TC", "TC-UNIT-LearningService-008")]
        [Trait("UC", "UC-24")]
        [Trait("BR", "BR-02")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task MarkCompleted_First_ProgressAndCommit()
        {
            var courseId = Guid.NewGuid();
            var materialId = Guid.NewGuid();
            var learnerId = Guid.NewGuid();
            var enrollment = new Enrollment(learnerId, courseId, 4);

            _enrollments.Setup(r => r.GetByCourseAndLearnerAsync(courseId, learnerId, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(enrollment);
            _materials.Setup(r => r.IsMaterialInCourseAsync(materialId, courseId, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(true);
            _progresses.Setup(r => r.GetByCompositeKeyAsync(enrollment.Id, materialId, It.IsAny<CancellationToken>()))
                       .ReturnsAsync((LearningProgress?)null);

            LearningProgress? created = null;
            _progresses.Setup(r => r.AddAsync(It.IsAny<LearningProgress>(), It.IsAny<CancellationToken>()))
                       .Callback<LearningProgress, CancellationToken>((p, _) => created = p)
                       .Returns(Task.CompletedTask);

            var result = await MarkCompletedHandler().Handle(
                new MarkMaterialAsCompletedCommand(courseId, materialId, learnerId), CancellationToken.None);

            Assert.True(result.Success);
            Assert.NotNull(created);
            Assert.True(created!.IsCompleted);
            Assert.Equal(1, enrollment.CompletedMaterials);
            _enrollments.Verify(r => r.Update(enrollment), Times.Once);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            _uow.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
            _uow.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        // ------------------------------------------------------------ TC-008 (nhánh chặn)
        // Hai lời chặn này chạy TRƯỚC khi mở transaction.
        [Fact]
        [Trait("TC", "TC-UNIT-LearningService-009")]
        [Trait("UC", "UC-24")]
        [Trait("Type", "Security (GBR)")]
        [Trait("Priority", "High")]
        public async Task MarkCompleted_NotEnrolled_NoTx()
        {
            _enrollments.Setup(r => r.GetByCourseAndLearnerAsync(
                            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync((Enrollment?)null);

            var result = await MarkCompletedHandler().Handle(
                new MarkMaterialAsCompletedCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()),
                CancellationToken.None);

            Assert.False(result.Success);
            Assert.Equal("ENROLLMENT_NOT_FOUND", result.ErrorCode);
            _uow.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
            VerifyNotSaved();
        }

        [Fact]
        [Trait("TC", "TC-UNIT-LearningService-009")]
        [Trait("UC", "UC-24")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        public async Task MarkCompleted_NotInCourse_NoTx()
        {
            var courseId = Guid.NewGuid();
            var learnerId = Guid.NewGuid();
            _enrollments.Setup(r => r.GetByCourseAndLearnerAsync(courseId, learnerId, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new Enrollment(learnerId, courseId, 4));
            _materials.Setup(r => r.IsMaterialInCourseAsync(
                            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync(false);

            var result = await MarkCompletedHandler().Handle(
                new MarkMaterialAsCompletedCommand(courseId, Guid.NewGuid(), learnerId), CancellationToken.None);

            Assert.Equal("MATERIAL_NOT_FOUND", result.ErrorCode);
            _uow.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
            VerifyNotSaved();
        }

        // ============================================================ TC-009
        // Covers: BR-03 once only — đánh dấu hoàn thành lần hai KHÔNG được cộng tiến độ thêm.
        // Handler rollback transaction rồi vẫn trả Success (idempotent), đúng cách xử lý
        // double-click / gửi lặp.
        [Fact]
        [Trait("TC", "TC-UNIT-LearningService-009")]
        [Trait("UC", "UC-24")]
        [Trait("BR", "BR-03")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        public async Task MarkCompleted_Second_IdempotentNoDouble()
        {
            var courseId = Guid.NewGuid();
            var materialId = Guid.NewGuid();
            var learnerId = Guid.NewGuid();
            var enrollment = new Enrollment(learnerId, courseId, 4);
            enrollment.CompleteMaterial();                       // lần đầu đã hoàn thành

            var progress = new LearningProgress(enrollment.Id, materialId);
            progress.Complete();

            _enrollments.Setup(r => r.GetByCourseAndLearnerAsync(courseId, learnerId, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(enrollment);
            _materials.Setup(r => r.IsMaterialInCourseAsync(materialId, courseId, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(true);
            _progresses.Setup(r => r.GetByCompositeKeyAsync(enrollment.Id, materialId, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(progress);

            var result = await MarkCompletedHandler().Handle(
                new MarkMaterialAsCompletedCommand(courseId, materialId, learnerId), CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal(1, enrollment.CompletedMaterials);      // KHÔNG tăng lên 2
            _uow.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
            _uow.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
            VerifyNotSaved();
        }

        private static Course CourseStub(bool published)
        {
            var builder = new Common.Builders.CourseBuilder();
            return published ? builder.Published().Build() : builder.Build();
        }

        // ============================================================ TC-003
        // Covers: BR-01 init once.
        // ⚠ Notes workbook đúng: đây là query READ-ONLY, KHÔNG tạo LearningProgress. "Khởi tạo
        // tiến độ lần đầu" không xảy ra ở đây — progress chỉ sinh ra khi markCompleted/submitQuiz.
        // Lần đầu vào học, CurrentMaterialId mặc định = material đầu tiên theo
        // Module.OrderIndex → Material.OrderIndex.
        [Fact]
        [Trait("TC", "TC-UNIT-LearningService-004")]
        [Trait("UC", "UC-23")]
        [Trait("BR", "BR-01")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task LearningView_FirstVisit_FirstMaterial()
        {
            var course = new Common.Builders.CourseBuilder()
                .WithModule("Chương một", materialCount: 2)
                .WithModule("Chương hai", materialCount: 1)
                .Build();
            var firstMaterialId = course.Modules.OrderBy(m => m.OrderIndex)
                                                .First().Materials.OrderBy(x => x.OrderIndex)
                                                .First().Id;

            _courses.Setup(r => r.GetCourseWithFullContentAsync(course.Id)).ReturnsAsync(course);
            _progresses.Setup(r => r.GetCompletedMaterialIdsAsync(course.Id, It.IsAny<Guid>()))
                       .ReturnsAsync(new List<Guid>());
            _progresses.Setup(r => r.GetCurrentMaterialIdAsync(course.Id, It.IsAny<Guid>()))
                       .ReturnsAsync((Guid?)null);

            var result = await LearningViewHandler().Handle(
                new GetCourseLearningViewQuery(course.Id, Guid.NewGuid()), CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal(firstMaterialId, result.Data!.Progress.CurrentMaterialId);
            Assert.Equal(0, result.Data.Progress.CompletedMaterials);
            Assert.Equal(3, result.Data.Progress.TotalMaterials);
            Assert.Equal(0, result.Data.Progress.Percent);

            // Query thuần đọc: không tạo tiến độ, không lưu gì.
            _progresses.Verify(r => r.AddAsync(It.IsAny<LearningProgress>(), It.IsAny<CancellationToken>()), Times.Never);
            VerifyNotSaved();
        }

        // ============================================================ TC-004
        // Covers: BR-02 resume — có tiến độ đã lưu thì lấy đúng material đang học dở,
        // không quay về material đầu tiên.
        [Fact]
        [Trait("TC", "TC-UNIT-LearningService-005")]
        [Trait("UC", "UC-23")]
        [Trait("BR", "BR-02")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        public async Task LearningView_SavedProgress_Resumes()
        {
            var course = new Common.Builders.CourseBuilder()
                .WithModule("Chương một", materialCount: 3)
                .Build();
            var materials = course.Modules.First().Materials.OrderBy(x => x.OrderIndex).ToList();
            var completedId = materials[0].Id;
            var resumeId = materials[1].Id;

            _courses.Setup(r => r.GetCourseWithFullContentAsync(course.Id)).ReturnsAsync(course);
            _progresses.Setup(r => r.GetCompletedMaterialIdsAsync(course.Id, It.IsAny<Guid>()))
                       .ReturnsAsync(new List<Guid> { completedId });
            _progresses.Setup(r => r.GetCurrentMaterialIdAsync(course.Id, It.IsAny<Guid>()))
                       .ReturnsAsync(resumeId);

            var result = await LearningViewHandler().Handle(
                new GetCourseLearningViewQuery(course.Id, Guid.NewGuid()), CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal(resumeId, result.Data!.Progress.CurrentMaterialId);   // KHÔNG phải materials[0]
            Assert.Equal(1, result.Data.Progress.CompletedMaterials);
            Assert.Equal(3, result.Data.Progress.TotalMaterials);
            Assert.Equal(100.0 / 3, result.Data.Progress.Percent, 3);

            var flat = result.Data.Modules.SelectMany(m => m.Materials).ToList();
            Assert.True(flat.Single(m => m.Id == completedId).IsCompleted);
            Assert.False(flat.Single(m => m.Id == resumeId).IsCompleted);
        }

        // ============================================================ TC-005 (phần course rỗng)
        // Covers: AF-01. Khoá học chưa có học liệu nào → view rỗng, Percent = 0
        // (tránh chia cho 0), CurrentMaterialId = null. KHÔNG phải lỗi.
        [Fact]
        [Trait("TC", "TC-UNIT-LearningService-005")]
        [Trait("UC", "UC-23")]
        [Trait("Type", "Boundary & Negative")]
        [Trait("Priority", "Medium")]
        public async Task LearningView_NoMaterial_ZeroPercent()
        {
            var course = new Common.Builders.CourseBuilder()
                .WithModule("Chương rỗng", materialCount: 0)
                .Build();

            _courses.Setup(r => r.GetCourseWithFullContentAsync(course.Id)).ReturnsAsync(course);
            _progresses.Setup(r => r.GetCompletedMaterialIdsAsync(course.Id, It.IsAny<Guid>()))
                       .ReturnsAsync(new List<Guid>());
            _progresses.Setup(r => r.GetCurrentMaterialIdAsync(course.Id, It.IsAny<Guid>()))
                       .ReturnsAsync((Guid?)null);

            var result = await LearningViewHandler().Handle(
                new GetCourseLearningViewQuery(course.Id, Guid.NewGuid()), CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal(0, result.Data!.Progress.TotalMaterials);
            Assert.Equal(0, result.Data.Progress.Percent);
            Assert.Null(result.Data.Progress.CurrentMaterialId);
            Assert.Single(result.Data.Modules);
        }
    }
}
