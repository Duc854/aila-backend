using AILA.Application.Common.Interfaces;
using AILA.Application.Common.Interfaces.Repositories;
using AILA.Application.Features.Reports.Commands.ReportCourse;
using AILA.Application.Features.Reports.Commands.ResolveReport;
using AILA.Application.Features.Reports.Queries.GetReports;
using AILA.Application.Tests.Common.Builders;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using Moq;

namespace AILA.Application.Tests.Features.Reports
{
    /// <summary>
    /// Sheet: ReportService · UC-33 / UC-79 · TC-UNIT-ReportService-001 → 010.
    /// </summary>
    public class ReportHandlerTests
    {
        private static readonly Guid LearnerId = Guid.NewGuid();

        private readonly Mock<ICourseRepository> _courses = new();
        private readonly Mock<IEnrollmentRepository> _enrollments = new();
        private readonly Mock<IMaterialRepository> _materials = new();
        private readonly Mock<IContentReportRepository> _reports = new();
        private readonly Mock<IUnitOfWork> _uow = new();

        public ReportHandlerTests()
        {
            _uow.Setup(u => u.Courses).Returns(_courses.Object);
            _uow.Setup(u => u.Enrollments).Returns(_enrollments.Object);
            _uow.Setup(u => u.Materials).Returns(_materials.Object);
            _uow.Setup(u => u.ContentReports).Returns(_reports.Object);
        }

        private ReportCourseCommandHandler ReportHandler() => new(_uow.Object);
        private ResolveReportCommandHandler ResolveHandler() => new(_uow.Object);
        private GetReportsQueryHandler GetReportsHandler() => new(_uow.Object);

        private void VerifyNotSaved()
            => _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

        /// <summary>Học viên đã ghi danh vào một khoá học tồn tại — tiền đề chung của UC-33.</summary>
        private Course ArrangeEnrolledCourse()
        {
            var course = new CourseBuilder().Published().Build();
            _courses.Setup(r => r.GetByIdAsync(course.Id)).ReturnsAsync(course);
            _enrollments.Setup(r => r.GetByCourseAndLearnerAsync(course.Id, LearnerId, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new Enrollment(LearnerId, course.Id, 4));
            return course;
        }

        // ============================================================ TC-001
        // Covers: Main Flow / BR-04 — báo cáo mới LUÔN ở trạng thái Pending.
        [Fact]
        [Trait("TC", "TC-UNIT-ReportService-001")]
        [Trait("UC", "UC-33")]
        [Trait("BR", "BR-04")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task Report_ValidCourse_CreatesPending()
        {
            var course = ArrangeEnrolledCourse();
            _reports.Setup(r => r.HasPendingReportAsync(
                        LearnerId, course.Id, null, It.IsAny<CancellationToken>())).ReturnsAsync(false);

            ContentReport? added = null;
            _reports.Setup(r => r.AddAsync(It.IsAny<ContentReport>()))
                    .Callback<ContentReport>(x => added = x)
                    .Returns(Task.CompletedTask);

            var result = await ReportHandler().Handle(
                new ReportCourseCommand(course.Id, null, LearnerId, ReportType.Other, "Nội dung sai"),
                CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal("Pending", result.Data!.Status);
            Assert.NotNull(added);
            Assert.Equal(ReportStatus.Pending, added!.Status);
            Assert.Equal(course.Id, added.CourseId);
            Assert.Null(added.MaterialId);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        // ============================================================ TC-004
        // Covers: BR-01 XOR — báo cáo gắn Course HOẶC Material, không bao giờ cả hai.
        // Nhánh if/else của handler là nơi bảo đảm điều đó.
        [Fact]
        [Trait("TC", "TC-UNIT-ReportService-004")]
        [Trait("UC", "UC-33")]
        [Trait("BR", "BR-01")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        public async Task Report_WithMaterialId_CourseIdNull()
        {
            var course = ArrangeEnrolledCourse();
            var materialId = Guid.NewGuid();
            _materials.Setup(r => r.IsMaterialInCourseAsync(materialId, course.Id, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(true);
            _reports.Setup(r => r.HasPendingReportAsync(
                        LearnerId, null, materialId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

            ContentReport? added = null;
            _reports.Setup(r => r.AddAsync(It.IsAny<ContentReport>()))
                    .Callback<ContentReport>(x => added = x)
                    .Returns(Task.CompletedTask);

            var result = await ReportHandler().Handle(
                new ReportCourseCommand(course.Id, materialId, LearnerId, ReportType.Other, null),
                CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal(materialId, added!.MaterialId);
            Assert.Null(added.CourseId);   // XOR: báo cáo học liệu thì không gắn khoá học
        }

        // Học liệu không thuộc khoá học được nêu → từ chối, không tiết lộ nó có tồn tại hay không.
        [Fact]
        [Trait("TC", "TC-UNIT-ReportService-004")]
        [Trait("UC", "UC-33")]
        [Trait("Type", "Security (GBR)")]
        [Trait("Priority", "Medium")]
        public async Task Report_MaterialNotInCourse_Rejected()
        {
            var course = ArrangeEnrolledCourse();
            _materials.Setup(r => r.IsMaterialInCourseAsync(
                        It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

            var result = await ReportHandler().Handle(
                new ReportCourseCommand(course.Id, Guid.NewGuid(), LearnerId, ReportType.Other, null),
                CancellationToken.None);

            Assert.Equal("MATERIAL_NOT_FOUND", result.ErrorCode);
            _reports.Verify(r => r.AddAsync(It.IsAny<ContentReport>()), Times.Never);
            VerifyNotSaved();
        }

        // ============================================================ TC-002 / TC-005  ⚠ DEFECT
        // BR-02 "chỉ 1 report/resource tại một thời điểm" ĐƯỢC enforce đúng (ALREADY_REPORTED).
        // Nhưng AF-01 của UCS nói khi đã có report Pending thì cho phép CẬP NHẬT mô tả —
        // code chỉ từ chối, không có đường sửa. Người dùng bổ sung thông tin không được.
        [Fact(Skip = "DEF-RPT-01 - A pending report cannot be updated, only rejected")]
        [Trait("TC", "TC-UNIT-ReportService-005")]
        [Trait("UC", "UC-33")]
        [Trait("BR", "BR-02")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        [Trait("Defect", "DEF-RPT-01")]
        public async Task Report_PendingExists_RejectedNoUpdate()
        {
            var course = ArrangeEnrolledCourse();
            _reports.Setup(r => r.HasPendingReportAsync(
                        LearnerId, course.Id, null, It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var result = await ReportHandler().Handle(
                new ReportCourseCommand(course.Id, null, LearnerId, ReportType.Other, "Bổ sung thông tin"),
                CancellationToken.None);

            Assert.False(result.Success);
            Assert.Equal("ALREADY_REPORTED", result.ErrorCode);
            _reports.Verify(r => r.AddAsync(It.IsAny<ContentReport>()), Times.Never);
            VerifyNotSaved();
        }

        // ============================================================ TC-003
        // Covers: AF-02. HasPendingReportAsync CHỈ xét trạng thái Pending, nên sau khi báo cáo
        // cũ đã Resolved, học viên báo cáo lại được — đúng ý "resolved không khoá vĩnh viễn".
        [Fact]
        [Trait("TC", "TC-UNIT-ReportService-003")]
        [Trait("UC", "UC-33")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        public async Task Report_AfterResolved_CanReportAgain()
        {
            var course = ArrangeEnrolledCourse();
            // Báo cáo cũ đã Resolved ⇒ repo không còn coi là "pending".
            _reports.Setup(r => r.HasPendingReportAsync(
                        LearnerId, course.Id, null, It.IsAny<CancellationToken>())).ReturnsAsync(false);

            var result = await ReportHandler().Handle(
                new ReportCourseCommand(course.Id, null, LearnerId, ReportType.Other, "Lại vi phạm"),
                CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal("Pending", result.Data!.Status);
            _reports.Verify(r => r.AddAsync(It.IsAny<ContentReport>()), Times.Once);
        }

        // ------------------------------------------------------------ Nhánh chặn của UC-33
        [Fact]
        [Trait("TC", "TC-UNIT-ReportService-001")]
        [Trait("UC", "UC-33")]
        [Trait("Type", "Security (GBR)")]
        [Trait("Priority", "High")]
        public async Task Report_NotEnrolled_Rejected()
        {
            var course = new CourseBuilder().Published().Build();
            _courses.Setup(r => r.GetByIdAsync(course.Id)).ReturnsAsync(course);
            _enrollments.Setup(r => r.GetByCourseAndLearnerAsync(
                        It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync((Enrollment?)null);

            var result = await ReportHandler().Handle(
                new ReportCourseCommand(course.Id, null, LearnerId, ReportType.Other, null),
                CancellationToken.None);

            Assert.Equal("NOT_ENROLLED", result.ErrorCode);
            _reports.Verify(r => r.AddAsync(It.IsAny<ContentReport>()), Times.Never);
            VerifyNotSaved();
        }

        [Fact]
        [Trait("TC", "TC-UNIT-ReportService-001")]
        [Trait("UC", "UC-33")]
        [Trait("Type", "Input Validation")]
        [Trait("Priority", "Medium")]
        public async Task Report_InvalidReason_RejectedNoDb()
        {
            var result = await ReportHandler().Handle(
                new ReportCourseCommand(Guid.NewGuid(), null, LearnerId, (ReportType)999, null),
                CancellationToken.None);

            Assert.Equal("INVALID_REASON", result.ErrorCode);
            _courses.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
            VerifyNotSaved();
        }

        // Biên độ dài mô tả: 1000 ký tự hợp lệ, 1001 bị chặn. Đo SAU Trim.
        [Theory]
        [InlineData(1000, true)]
        [InlineData(1001, false)]
        [Trait("TC", "TC-UNIT-ReportService-001")]
        [Trait("UC", "UC-33")]
        [Trait("Type", "Boundary & Negative")]
        [Trait("Priority", "Medium")]
        public async Task Report_DescriptionAtLengthBoundary(int length, bool shouldSucceed)
        {
            var course = ArrangeEnrolledCourse();
            _reports.Setup(r => r.HasPendingReportAsync(
                        It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(false);

            var result = await ReportHandler().Handle(
                new ReportCourseCommand(course.Id, null, LearnerId, ReportType.Other, new string('a', length)),
                CancellationToken.None);

            Assert.Equal(shouldSucceed, result.Success);
            if (!shouldSucceed)
            {
                Assert.Equal("VALIDATION_ERROR", result.ErrorCode);
                VerifyNotSaved();
            }
        }

        // ============================================================ TC-007
        // Covers: BR-01/02 filter.
        // Phạm vi L1: lọc thực nằm ở ContentReports.GetReportsAsync — ở đây chỉ khẳng định
        // handler truyền đúng tham số xuống repo.
        [Theory]
        [InlineData(ReportStatus.Pending, true)]
        [InlineData(ReportStatus.Resolved, false)]
        [InlineData(null, null)]
        [Trait("TC", "TC-UNIT-ReportService-007")]
        [Trait("UC", "UC-79")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        public async Task GetReports_ForwardsFiltersToRepository(ReportStatus? status, bool? isCourseReport)
        {
            _reports.Setup(r => r.GetReportsAsync(status, isCourseReport, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<ContentReport>());

            var result = await GetReportsHandler().Handle(
                new GetReportsQuery(status, isCourseReport), CancellationToken.None);

            Assert.True(result.Success);
            _reports.Verify(r => r.GetReportsAsync(status, isCourseReport, It.IsAny<CancellationToken>()), Times.Once);
        }

        // ============================================================ TC-008
        // Covers: AF-01 — không khớp gì là danh sách rỗng, KHÔNG phải lỗi.
        [Fact]
        [Trait("TC", "TC-UNIT-ReportService-008")]
        [Trait("UC", "UC-79")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Low")]
        public async Task GetReports_NoMatch_EmptyList()
        {
            _reports.Setup(r => r.GetReportsAsync(
                        It.IsAny<ReportStatus?>(), It.IsAny<bool?>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<ContentReport>());

            var result = await GetReportsHandler().Handle(
                new GetReportsQuery(ReportStatus.Resolved, false), CancellationToken.None);

            Assert.True(result.Success);
            Assert.Empty(result.Data!);
            Assert.Null(result.ErrorCode);
        }

        // ============================================================ TC-009
        // Covers: BR-04 PENDING → RESOLVED.
        [Fact]
        [Trait("TC", "TC-UNIT-ReportService-009")]
        [Trait("UC", "UC-79")]
        [Trait("BR", "BR-04")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task Resolve_Pending_MarksResolvedWithTime()
        {
            var report = new ContentReport(LearnerId, Guid.NewGuid(), null, ReportType.Other, "Nội dung sai");
            _reports.Setup(r => r.GetByIdAsync(report.Id)).ReturnsAsync(report);

            var result = await ResolveHandler().Handle(
                new ResolveReportCommand { ReportId = report.Id }, CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal(ReportStatus.Resolved, report.Status);
            Assert.NotNull(report.ResolvedAt);
            Assert.Equal("Resolved", result.Data!.Status);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        // ============================================================ TC-010
        // Covers: BR-04 chỉ Pending mới resolve được.
        // Handler chặn TRƯỚC khi gọi domain — nếu không, ContentReport.Resolve() sẽ ném
        // InvalidOperationException và lộ ra thành lỗi 500 thay vì mã lỗi thân thiện.
        [Fact]
        [Trait("TC", "TC-UNIT-ReportService-010")]
        [Trait("UC", "UC-79")]
        [Trait("BR", "BR-04")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        public async Task Resolve_AlreadyResolved_KeepsState()
        {
            var report = new ContentReport(LearnerId, Guid.NewGuid(), null, ReportType.Other, null);
            report.Resolve();
            var resolvedAt = report.ResolvedAt;

            _reports.Setup(r => r.GetByIdAsync(report.Id)).ReturnsAsync(report);

            var result = await ResolveHandler().Handle(
                new ResolveReportCommand { ReportId = report.Id }, CancellationToken.None);

            Assert.False(result.Success);
            Assert.Equal("ALREADY_RESOLVED", result.ErrorCode);
            Assert.Equal(resolvedAt, report.ResolvedAt);   // mốc thời gian cũ không bị ghi đè
            VerifyNotSaved();
        }

        [Fact]
        [Trait("TC", "TC-UNIT-ReportService-010")]
        [Trait("UC", "UC-79")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        public async Task Resolve_ReportNotFound_ReturnsReportNotFound()
        {
            _reports.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((ContentReport?)null);

            var result = await ResolveHandler().Handle(
                new ResolveReportCommand { ReportId = Guid.NewGuid() }, CancellationToken.None);

            Assert.Equal("REPORT_NOT_FOUND", result.ErrorCode);
            VerifyNotSaved();
        }

        [Fact]
        [Trait("TC", "TC-UNIT-ReportService-010")]
        [Trait("UC", "UC-79")]
        [Trait("Type", "Input Validation")]
        [Trait("Priority", "Low")]
        public async Task Resolve_EmptyId_RejectedNoDb()
        {
            var result = await ResolveHandler().Handle(
                new ResolveReportCommand { ReportId = Guid.Empty }, CancellationToken.None);

            Assert.Equal("INVALID_REPORT_ID", result.ErrorCode);
            _reports.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
            VerifyNotSaved();
        }
    }
}
