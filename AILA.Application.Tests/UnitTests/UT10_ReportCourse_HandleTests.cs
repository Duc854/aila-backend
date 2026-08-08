using AILA.Application.Common.Interfaces;
using AILA.Application.Common.Interfaces.Repositories;
using AILA.Application.Features.Reports.Commands.ReportCourse;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using Moq;

namespace AILA.Application.Tests.UnitTests;

/// <summary>
/// Sheet: UT10_ReportCourse — <see cref="ReportCourseCommandHandler.Handle"/>
/// Module: Moderation · CC = 10 · 13 test case
///
/// Nhánh: B1 = Reason ngoài enum · B2 = toán tử ?. khi Description null
///        B3 = mô tả &gt; 1000 ký tự · B4 = course null · B5 = chưa enroll
///        B6 = có MaterialId · B7 = học liệu không thuộc khoá · B8 = đã có báo cáo Pending
///        B9 = chọn thông điệp theo đối tượng bị báo cáo
///
/// HỢP ĐỒNG DỮ LIỆU (đổi từ commit 2ff5f51 "refactor: update content report relationship"):
/// ContentReport.CourseId nay là NON-NULLABLE — mọi báo cáo đều thuộc về một khóa học;
/// MaterialId là tuỳ chọn, chỉ ra học liệu cụ thể bị báo cáo. Bất biến XOR cũ đã bị bỏ.
/// Handler chưa được cập nhật theo hợp đồng mới ⇒ xem UTCID09 (DFID003).
/// </summary>
public class UT10_ReportCourse_HandleTests
{
    private static readonly Guid LearnerId = Guid.NewGuid();
    private static readonly Guid MaterialId = Guid.NewGuid();

    private static readonly string Description1000 = new('x', 1000);
    private static readonly string Description1001 = new('x', 1001);

    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ICourseRepository> _courses = new();
    private readonly Mock<IEnrollmentRepository> _enrollments = new();
    private readonly Mock<IMaterialRepository> _materials = new();
    private readonly Mock<IContentReportRepository> _reports = new();

    private readonly Course _course = new("Prompt Engineering 101", Guid.NewGuid(), Guid.NewGuid(), KnowledgeLevel.Beginner);

    public UT10_ReportCourse_HandleTests()
    {
        _uow.SetupGet(x => x.Courses).Returns(_courses.Object);
        _uow.SetupGet(x => x.Enrollments).Returns(_enrollments.Object);
        _uow.SetupGet(x => x.Materials).Returns(_materials.Object);
        _uow.SetupGet(x => x.ContentReports).Returns(_reports.Object);
        _uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _reports.Setup(x => x.HasPendingReportAsync(
                    It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
    }

    private ReportCourseCommandHandler CreateSut() => new(_uow.Object);

    private void SetupCourse(Course? course) =>
        _courses.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(course);

    private void SetupEnrollment(Enrollment? enrollment) =>
        _enrollments.Setup(x => x.GetByCourseAndLearnerAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(enrollment);

    private void SetupEnrolled()
    {
        SetupCourse(_course);
        SetupEnrollment(new Enrollment(LearnerId, _course.Id, 5));
    }

    private Task<Shared.Wrappers.ResponseDto<Features.Reports.Dtos.ReportCourseResponseDto>> Act(
        ReportType reason, string? description = null, Guid? materialId = null) =>
        CreateSut().Handle(
            new ReportCourseCommand(_course.Id, materialId, LearnerId, reason, description),
            CancellationToken.None);

    private void AssertNothingPersisted()
    {
        _reports.Verify(x => x.AddAsync(It.IsAny<ContentReport>()), Times.Never);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>UTCID01 · B1=T · Type B — Reason = 0 (giá trị mặc định của enum, biên dưới không hợp lệ).</summary>
    [Fact]
    public async Task UTCID01_ReasonZero_ReturnsInvalidReason()
    {
        var result = await Act((ReportType)0);

        Assert.False(result.Success);
        Assert.Equal("INVALID_REASON", result.ErrorCode);
        AssertNothingPersisted();
    }

    /// <summary>UTCID02 · B1=T · Type B — Reason = 9 (biên trên không hợp lệ, enum tối đa là 8).</summary>
    [Fact]
    public async Task UTCID02_ReasonNine_ReturnsInvalidReason()
    {
        var result = await Act((ReportType)9);

        Assert.False(result.Success);
        Assert.Equal("INVALID_REASON", result.ErrorCode);
        AssertNothingPersisted();
    }

    /// <summary>UTCID03 · B3=T · Type B — mô tả 1001 ký tự (biên trên không hợp lệ).</summary>
    [Fact]
    public async Task UTCID03_DescriptionLength1001_ReturnsValidationError()
    {
        var result = await Act(ReportType.InappropriateContent, Description1001);

        Assert.False(result.Success);
        Assert.Equal("VALIDATION_ERROR", result.ErrorCode);
        Assert.Equal("Mô tả không được vượt quá 1000 ký tự.", result.ErrorMessage);
        AssertNothingPersisted();
    }

    /// <summary>UTCID04 · B3=F, B4=T · Type B — mô tả 1000 ký tự (biên trên hợp lệ) nhưng khoá học không tồn tại.</summary>
    [Fact]
    public async Task UTCID04_DescriptionLength1000WithMissingCourse_ReturnsCourseNotFound()
    {
        SetupCourse(null);

        var result = await Act(ReportType.InappropriateContent, Description1000);

        Assert.False(result.Success);
        Assert.Equal("COURSE_NOT_FOUND", result.ErrorCode);
    }

    /// <summary>UTCID05 · B1=F · Type B — Reason = 8 (Other, biên trên hợp lệ của enum).</summary>
    [Fact]
    public async Task UTCID05_ReasonOther_PassesEnumValidation()
    {
        SetupCourse(null);

        var result = await Act(ReportType.Other);

        Assert.False(result.Success);
        Assert.Equal("COURSE_NOT_FOUND", result.ErrorCode);
    }

    /// <summary>UTCID06 · B4=F, B5=T · Type A — chưa tham gia khoá học.</summary>
    [Fact]
    public async Task UTCID06_NotEnrolled_ReturnsNotEnrolled()
    {
        SetupCourse(_course);
        SetupEnrollment(null);

        var result = await Act(ReportType.Spam);

        Assert.False(result.Success);
        Assert.Equal("NOT_ENROLLED", result.ErrorCode);
        Assert.Equal("Bạn cần tham gia khóa học này trước khi báo cáo.", result.ErrorMessage);
        AssertNothingPersisted();
    }

    /// <summary>
    /// UTCID07 · B6=F, B8=F · Type N — báo cáo CẢ KHOÁ HỌC.
    /// report.CourseId = CourseId, report.MaterialId = null.
    /// </summary>
    [Fact]
    public async Task UTCID07_ReportWholeCourse_CreatesPendingReportWithCourseIdOnly()
    {
        SetupEnrolled();

        var result = await Act(ReportType.Spam, "Nội dung spam");

        Assert.True(result.Success);
        Assert.Equal("Pending", result.Data!.Status);
        _reports.Verify(x => x.HasPendingReportAsync(LearnerId, _course.Id, null, It.IsAny<CancellationToken>()), Times.Once);
        _reports.Verify(x => x.AddAsync(It.Is<ContentReport>(r =>
            r.CourseId == _course.Id && r.MaterialId == null && r.Status == ReportStatus.Pending)), Times.Once);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>UTCID08 · B6=T, B7=T · Type A — học liệu không thuộc khoá học đang báo cáo.</summary>
    [Fact]
    public async Task UTCID08_MaterialNotInCourse_ReturnsMaterialNotFound()
    {
        SetupEnrolled();
        _materials.Setup(x => x.IsMaterialInCourseAsync(MaterialId, _course.Id, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(false);

        var result = await Act(ReportType.Spam, materialId: MaterialId);

        Assert.False(result.Success);
        Assert.Equal("MATERIAL_NOT_FOUND", result.ErrorCode);
        _reports.Verify(x => x.HasPendingReportAsync(
            It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);
        AssertNothingPersisted();
    }

    /// <summary>
    /// UTCID09 · B7=F, B8=F · Type N — báo cáo MỘT HỌC LIỆU.
    ///
    /// DEFECT DFID003 (regression từ commit 2ff5f51): ContentReport.CourseId nay là
    /// non-nullable, nhưng handler vẫn gán reportCourseId = null cho nhánh học liệu rồi gọi
    /// reportCourseId.Value ở dòng 71 ⇒ ném InvalidOperationException("Nullable object must
    /// have a value") ⇒ toàn bộ chức năng "báo cáo học liệu" trả HTTP 500.
    ///
    /// Expected ghi theo hợp đồng ĐÚNG: report phải mang CẢ CourseId lẫn MaterialId.
    /// ⇒ Test này DỰ KIẾN FAIL cho tới khi handler được sửa.
    /// </summary>
    [Fact]
    public async Task UTCID09_ReportMaterial_CreatesPendingReportWithCourseIdAndMaterialId()
    {
        SetupEnrolled();
        _materials.Setup(x => x.IsMaterialInCourseAsync(MaterialId, _course.Id, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(true);

        var result = await Act(ReportType.Violence, "Video bạo lực", MaterialId);

        Assert.True(result.Success);
        Assert.Equal("Pending", result.Data!.Status);
        _reports.Verify(x => x.AddAsync(It.Is<ContentReport>(r =>
            r.CourseId == _course.Id && r.MaterialId == MaterialId)), Times.Once);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>UTCID10 · B8=T, B9=F · Type A — đã có báo cáo Pending cho KHOÁ HỌC.</summary>
    [Fact]
    public async Task UTCID10_DuplicateCourseReport_ReturnsAlreadyReportedWithCourseMessage()
    {
        SetupEnrolled();
        _reports.Setup(x => x.HasPendingReportAsync(LearnerId, _course.Id, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

        var result = await Act(ReportType.Spam);

        Assert.False(result.Success);
        Assert.Equal("ALREADY_REPORTED", result.ErrorCode);
        Assert.Equal("Bạn đã báo cáo khóa học này và đang chờ xử lý.", result.ErrorMessage);
        AssertNothingPersisted();
    }

    /// <summary>
    /// UTCID11 · B8=T, B9=T · Type A — đã có báo cáo Pending cho HỌC LIỆU.
    /// Không ràng buộc tham số cụ thể của HasPendingReportAsync: khoá chống trùng cho học liệu
    /// vẫn xác định duy nhất bằng MaterialId, việc handler truyền courseId = null chỉ là
    /// Observation (không phải defect) — xem Test Design §B.
    /// </summary>
    [Fact]
    public async Task UTCID11_DuplicateMaterialReport_ReturnsAlreadyReportedWithMaterialMessage()
    {
        SetupEnrolled();
        _materials.Setup(x => x.IsMaterialInCourseAsync(MaterialId, _course.Id, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(true);
        _reports.Setup(x => x.HasPendingReportAsync(
                    LearnerId, It.IsAny<Guid?>(), MaterialId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

        var result = await Act(ReportType.Spam, materialId: MaterialId);

        Assert.False(result.Success);
        Assert.Equal("ALREADY_REPORTED", result.ErrorCode);
        Assert.Equal("Bạn đã báo cáo học liệu này và đang chờ xử lý.", result.ErrorMessage);
        AssertNothingPersisted();
    }

    /// <summary>
    /// UTCID12 · B3=F · Type B — mô tả 1000 ký tự bọc khoảng trắng.
    /// Trim() chạy TRƯỚC khi đo độ dài ⇒ vẫn hợp lệ.
    /// </summary>
    [Fact]
    public async Task UTCID12_DescriptionTrimmedTo1000_IsAccepted()
    {
        SetupEnrolled();

        var result = await Act(ReportType.HateSpeech, $"   {Description1000}   ");

        Assert.True(result.Success);
        _reports.Verify(x => x.AddAsync(It.Is<ContentReport>(r =>
            r.Description != null && r.Description.Length == 1000)), Times.Once);
    }

    /// <summary>UTCID13 · B2 (toán tử ?.), B3=F · Type B — mô tả rỗng.</summary>
    [Fact]
    public async Task UTCID13_EmptyDescription_IsAccepted()
    {
        SetupEnrolled();

        var result = await Act(ReportType.IncorrectInformation, string.Empty);

        Assert.True(result.Success);
        _reports.Verify(x => x.AddAsync(It.Is<ContentReport>(r => r.Description == string.Empty)), Times.Once);
    }
}
