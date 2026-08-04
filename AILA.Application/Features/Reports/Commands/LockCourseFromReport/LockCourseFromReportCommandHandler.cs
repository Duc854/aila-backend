using AILA.Application.Common.Interfaces;
using AILA.Application.Features.Reports.Dtos;
using AILA.Domain.Enums;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Reports.Commands.LockCourseFromReport;

public sealed class LockCourseFromReportCommandHandler
    : IRequestHandler<LockCourseFromReportCommand, ResponseDto<CourseModerationResponseDto>>
{
    private readonly IUnitOfWork _uow;

    public LockCourseFromReportCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<ResponseDto<CourseModerationResponseDto>> Handle(
        LockCourseFromReportCommand request,
        CancellationToken ct)
    {
        // 1. Load report kèm Course (tracked)
        var report = await _uow.ContentReports
            .GetReportWithCourseForUpdateAsync(request.ReportId, ct);

        if (report is null)
            return ResponseDto<CourseModerationResponseDto>.FailResult(
                "REPORT_NOT_FOUND", "Không tìm thấy báo cáo.");

        // 2. Resolve course từ report — hỗ trợ cả course report lẫn content report
        var course = report.Course                          // course report
                  ?? report.Material?.Module?.Course;      // content report

        if (course is null)
            return ResponseDto<CourseModerationResponseDto>.FailResult(
                "NOT_COURSE_REPORT", "Báo cáo này không liên quan đến khóa học.");

        // 3. Report phải đang Pending
        if (report.Status != ReportStatus.Pending)
            return ResponseDto<CourseModerationResponseDto>.FailResult(
                "ALREADY_RESOLVED", "Báo cáo đã được xử lý trước đó.");

        // 4. Domain actions
        course.LockVisibility();
        report.Resolve();

        await _uow.SaveChangesAsync(ct);

        return ResponseDto<CourseModerationResponseDto>.SuccessResult(
            new CourseModerationResponseDto
            {
                CourseId            = course.Id,
                CourseName          = course.Name,
                IsPublished         = course.IsPublished,
                IsPublicationLocked = course.IsPublicationLocked,
                Message             = "Khóa học đã bị khoá và báo cáo đã được đánh dấu xử lý."
            });
    }
}
