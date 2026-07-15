using AILA.Application.Features.Reports.Dtos;
using AILA.Domain.Enums;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Reports.Commands.ReportCourse
{
    /// <summary>UC-33 — Learner báo cáo một khóa học đang xem. Tạo report Pending → moderation queue.</summary>
    public record ReportCourseCommand(
        Guid CourseId,
        Guid? MaterialId,
        Guid LearnerId,
        ReportType Reason,
        string? Description
    ) : IRequest<ResponseDto<ReportCourseResponseDto>>;
}
