using AILA.Domain.Enums;

namespace AILA.Application.Features.Reports.Commands.ReportCourse
{
    public record ReportCourseRequest(
        ReportType Reason,
        string? Description
    );
}