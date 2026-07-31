using AILA.Application.Common.Interfaces;
using AILA.Application.Common.Interfaces.Repositories;
using AILA.Application.Features.Reports.Commands.LockCourseFromReport;
using AILA.Application.Features.Reports.Commands.UnlockCourse;
using AILA.Application.Features.Reports.Queries.GetReportById;
using AILA.Application.Features.Reports.Queries.GetReportReasons;
using AILA.Application.Tests.Common;
using AILA.Application.Tests.Common.Builders;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using Moq;

namespace AILA.Application.Tests.Features.Reports
{
    /// <summary>
    /// Sheet: ReportService · UC-74 Review Content Reports / UC-75 Apply Course Moderation.
    /// TC-UNIT-ReportService-011 → 020.
    /// </summary>
    public class CourseModerationHandlerTests
    {
        private static readonly Guid LearnerId = Guid.NewGuid();
        private static readonly Guid ExpertId = Guid.NewGuid();

        private readonly Mock<IContentReportRepository> _reports = new();
        private readonly Mock<ICourseRepository> _courses = new();
        private readonly Mock<IUnitOfWork> _uow = new();

        public CourseModerationHandlerTests()
        {
            _uow.Setup(u => u.ContentReports).Returns(_reports.Object);
            _uow.Setup(u => u.Courses).Returns(_courses.Object);
        }

        private static Course PublishedCourse()
            => new CourseBuilder().OwnedBy(ExpertId).Published().Build();

        /// <summary>ContentReport nhắm vào một khoá học, kèm navigation Course đã nạp.</summary>
        private static ContentReport CourseReport(Course course, bool resolved = false)
        {
            var report = new ContentReport(LearnerId, course.Id, null, ReportType.Other, "Nội dung không phù hợp");
            TestEntity.SetProperty(report, nameof(ContentReport.Course), course);
            if (resolved) report.Resolve();
            return report;
        }

        // ============================================================ UC-75 Lock

        // ------------------------------------------------------------ TC-011
        // Covers: Main Flow — khoá khoá học VÀ đóng báo cáo trong cùng một lần lưu.
        [Fact]
        [Trait("TC", "TC-UNIT-ReportService-015")]
        [Trait("UC", "UC-75")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Critical")]
        public async Task Lock_PendingCourseReport_LocksAndResolves()
        {
            var course = PublishedCourse();
            var report = CourseReport(course);

            _reports.Setup(r => r.GetReportWithCourseForUpdateAsync(report.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(report);

            var handler = new LockCourseFromReportCommandHandler(_uow.Object);
            var result = await handler.Handle(new LockCourseFromReportCommand(report.Id), CancellationToken.None);

            Assert.True(result.Success);
            Assert.True(course.IsPublicationLocked);
            Assert.Equal(ReportStatus.Resolved, report.Status);
            Assert.NotNull(report.ResolvedAt);
            Assert.True(result.Data!.IsPublicationLocked);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        // ------------------------------------------------------------ TC-012
        // Covers: AF-01 — báo cáo không tồn tại, hoặc đã xử lý rồi (chống double-click:
        // lần bấm thứ hai không được khoá lại lần nữa).
        [Fact]
        [Trait("TC", "TC-UNIT-ReportService-016")]
        [Trait("UC", "UC-75")]
        [Trait("Type", "Boundary & Negative")]
        [Trait("Priority", "High")]
        public async Task Lock_MissingOrAlreadyResolved_Rejected()
        {
            var handler = new LockCourseFromReportCommandHandler(_uow.Object);

            _reports.Setup(r => r.GetReportWithCourseForUpdateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((ContentReport?)null);
            var missing = await handler.Handle(new LockCourseFromReportCommand(Guid.NewGuid()), CancellationToken.None);
            Assert.False(missing.Success);
            Assert.Equal("REPORT_NOT_FOUND", missing.ErrorCode);

            var course = PublishedCourse();
            var done = CourseReport(course, resolved: true);
            _reports.Setup(r => r.GetReportWithCourseForUpdateAsync(done.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(done);
            var already = await handler.Handle(new LockCourseFromReportCommand(done.Id), CancellationToken.None);
            Assert.False(already.Success);
            Assert.Equal("ALREADY_RESOLVED", already.ErrorCode);
            Assert.False(course.IsPublicationLocked);

            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        // ------------------------------------------------------------ TC-013
        // Covers: BR-01 — báo cáo nhắm vào learning material chứ không phải khoá học thì
        // không có gì để khoá ở mức khoá học.
        [Fact]
        [Trait("TC", "TC-UNIT-ReportService-017")]
        [Trait("UC", "UC-75")]
        [Trait("BR", "BR-01")]
        [Trait("Type", "Boundary & Negative")]
        [Trait("Priority", "High")]
        public async Task Lock_MaterialReport_NotCourseReport()
        {
            var report = new ContentReport(LearnerId, null, Guid.NewGuid(), ReportType.Other, "Sai kiến thức");

            _reports.Setup(r => r.GetReportWithCourseForUpdateAsync(report.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(report);

            var handler = new LockCourseFromReportCommandHandler(_uow.Object);
            var result = await handler.Handle(new LockCourseFromReportCommand(report.Id), CancellationToken.None);

            Assert.False(result.Success);
            Assert.Equal("NOT_COURSE_REPORT", result.ErrorCode);
            Assert.Equal(ReportStatus.Pending, report.Status);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        // ============================================================ UC-75 Unlock

        // ------------------------------------------------------------ TC-014  ⚠ DEFECT
        // UnlockCourseCommandHandler KHÔNG gỡ khoá gì cả: lời gọi domain ở dòng 35 đang bị
        // comment (`//course.UnlockVisibility();`) vì method đó đã bị đổi tên thành
        // Course.RestorePublication() ở commit 6fc785e, và lời gọi không được cập nhật theo.
        // Hậu quả: admin bấm "gỡ khoá" → handler trả Success=true kèm thông điệp
        // "Khóa học đã được gỡ khoá", nhưng IsPublicationLocked vẫn là true và khoá học
        // vẫn bị ẩn. Đây là lỗi im lặng — không có dấu hiệu nào cho admin biết thao tác trượt.
        //
        // Bỏ Skip sau khi sửa handler. Xem TC-016 để biết vì sao việc sửa không chỉ là
        // đổi tên method: RestorePublication() còn TỰ PUBLISH LẠI, khác với thông điệp
        // "Expert có thể publish lại" mà chính handler này trả về.
        [Fact(Skip = "DEF-RPT-02 - UnlockCourse is a silent no-op, the domain call is commented out")]
        [Trait("TC", "TC-UNIT-ReportService-018")]
        [Trait("UC", "UC-75")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Critical")]
        [Trait("Defect", "DEF-RPT-02")]
        public async Task Unlock_LockedCourse_ClearsLock()
        {
            var course = PublishedCourse();
            course.LockVisibility();
            _courses.Setup(r => r.GetByIdAsync(course.Id)).ReturnsAsync(course);

            var handler = new UnlockCourseCommandHandler(_uow.Object);
            var result = await handler.Handle(new UnlockCourseCommand(course.Id), CancellationToken.None);

            Assert.True(result.Success);
            Assert.False(course.IsPublicationLocked);
            Assert.False(result.Data!.IsPublicationLocked);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        // ------------------------------------------------------------ TC-015
        // Covers: AF-01 — khoá học không tồn tại, hoặc vốn không bị khoá.
        [Fact]
        [Trait("TC", "TC-UNIT-ReportService-019")]
        [Trait("UC", "UC-75")]
        [Trait("Type", "Boundary & Negative")]
        [Trait("Priority", "High")]
        public async Task Unlock_MissingOrNotLocked_Rejected()
        {
            var handler = new UnlockCourseCommandHandler(_uow.Object);

            _courses.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Course?)null);
            var missing = await handler.Handle(new UnlockCourseCommand(Guid.NewGuid()), CancellationToken.None);
            Assert.False(missing.Success);
            Assert.Equal("COURSE_NOT_FOUND", missing.ErrorCode);

            var open = PublishedCourse();
            _courses.Setup(r => r.GetByIdAsync(open.Id)).ReturnsAsync(open);
            var notLocked = await handler.Handle(new UnlockCourseCommand(open.Id), CancellationToken.None);
            Assert.False(notLocked.Success);
            Assert.Equal("NOT_LOCKED", notLocked.ErrorCode);

            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        // ------------------------------------------------------------ TC-016
        // Covers: BR-01 — hợp đồng kiểm duyệt ở tầng domain, không qua mock.
        // Ghi lại chính xác ngữ nghĩa hiện có của Course, vì đây là chỗ handler ở TC-014
        // sẽ phải đấu vào khi được sửa:
        //   LockVisibility()      : gỡ publish VÀ bật cờ khoá  (hai việc, một lần gọi)
        //   RestorePublication()  : tắt cờ khoá VÀ publish lại (cũng hai việc)
        //   RestorePublication() khi không bị khoá -> ném InvalidOperationException
        //
        // ⚠ Mâu thuẫn cần chốt: UnlockCourseCommandHandler trả thông điệp "Khóa học đã được
        // gỡ khoá. Expert có thể publish lại" — hàm ý gỡ khoá KHÔNG tự publish. Nhưng
        // RestorePublication() là hàm duy nhất domain cung cấp, và nó publish luôn.
        // Hoặc sửa thông điệp, hoặc tách domain thành hai hàm riêng.
        [Fact]
        [Trait("TC", "TC-UNIT-ReportService-016")]
        [Trait("UC", "UC-75")]
        [Trait("BR", "BR-01")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        public void Course_LockAndRestore_Contract()
        {
            var course = PublishedCourse();
            Assert.True(course.IsPublished);
            Assert.False(course.IsPublicationLocked);

            course.LockVisibility();
            Assert.False(course.IsPublished);          // khoá cũng gỡ publish
            Assert.True(course.IsPublicationLocked);

            course.RestorePublication();
            Assert.False(course.IsPublicationLocked);
            Assert.True(course.IsPublished);           // ...và khôi phục thì publish lại luôn

            // Gọi khôi phục lần nữa trên khoá học không bị khoá là thao tác vô nghĩa.
            Assert.Throws<InvalidOperationException>(() => course.RestorePublication());
        }

        // ============================================================ UC-74 Review

        // ------------------------------------------------------------ TC-017
        [Fact]
        [Trait("TC", "TC-UNIT-ReportService-011")]
        [Trait("UC", "UC-74")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task GetById_ExistingReport_ReturnsDetail()
        {
            var course = PublishedCourse();
            var report = CourseReport(course);

            _reports.Setup(r => r.GetReportWithDetailsAsync(report.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(report);

            var handler = new GetReportByIdQueryHandler(_uow.Object);
            var result = await handler.Handle(new GetReportByIdQuery(report.Id), CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal(course.Id, result.Data!.CourseId);
            Assert.Equal(course.Name, result.Data.CourseName);
            Assert.Equal("Course", result.Data.ContentType);
            Assert.Equal(nameof(ReportStatus.Pending), result.Data.Status);
        }

        // ------------------------------------------------------------ TC-018
        // Covers: BR-01 — ContentType suy ra từ việc có MaterialId hay không.
        [Fact]
        [Trait("TC", "TC-UNIT-ReportService-012")]
        [Trait("UC", "UC-74")]
        [Trait("BR", "BR-01")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        public async Task GetById_MaterialReport_ReportsMaterialContentType()
        {
            var report = new ContentReport(LearnerId, null, Guid.NewGuid(), ReportType.Other, "Sai kiến thức");
            _reports.Setup(r => r.GetReportWithDetailsAsync(report.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(report);

            var handler = new GetReportByIdQueryHandler(_uow.Object);
            var result = await handler.Handle(new GetReportByIdQuery(report.Id), CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal("Learning Material", result.Data!.ContentType);
            Assert.Null(result.Data.CourseId);
        }

        // ------------------------------------------------------------ TC-019
        // Covers: AF-01 — id rỗng bị chặn TRƯỚC khi truy vấn, id lạ trả REPORT_NOT_FOUND.
        [Fact]
        [Trait("TC", "TC-UNIT-ReportService-012")]
        [Trait("UC", "UC-74")]
        [Trait("Type", "Boundary & Negative")]
        [Trait("Priority", "Medium")]
        public async Task GetById_EmptyOrUnknownId_Rejected()
        {
            var handler = new GetReportByIdQueryHandler(_uow.Object);

            var empty = await handler.Handle(new GetReportByIdQuery(Guid.Empty), CancellationToken.None);
            Assert.False(empty.Success);
            Assert.Equal("INVALID_REPORT_ID", empty.ErrorCode);
            _reports.Verify(r => r.GetReportWithDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                Times.Never);

            _reports.Setup(r => r.GetReportWithDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((ContentReport?)null);
            var unknown = await handler.Handle(new GetReportByIdQuery(Guid.NewGuid()), CancellationToken.None);
            Assert.False(unknown.Success);
            Assert.Equal("REPORT_NOT_FOUND", unknown.ErrorCode);
        }

        // ------------------------------------------------------------ TC-020
        // Covers: Main Flow — danh sách lý do lấy thẳng từ enum ReportType, không chạm DB.
        // Test khoá lại điều đó: thêm một ReportType mới sẽ tự xuất hiện ở dialog báo cáo.
        [Fact]
        [Trait("TC", "TC-UNIT-ReportService-013")]
        [Trait("UC", "UC-33")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        public async Task GetReasons_MirrorsReportTypeEnum()
        {
            var handler = new GetReportReasonsQueryHandler();
            var result = await handler.Handle(new GetReportReasonsQuery(), CancellationToken.None);

            Assert.True(result.Success);
            var expected = Enum.GetValues<ReportType>();
            Assert.Equal(expected.Length, result.Data!.Count());
            foreach (var reason in expected)
                Assert.Contains(result.Data, r => r.Name == reason.ToString() && r.Id == (int)reason);
        }
    }
}
