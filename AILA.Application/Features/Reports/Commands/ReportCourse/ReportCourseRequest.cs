using AILA.Domain.Enums;

namespace AILA.Application.Features.Reports.Commands.ReportCourse
{
    public record ReportCourseRequest(
        ReportType Reason,
        string? Description,
        // Tùy chọn: có giá trị → báo cáo một học liệu cụ thể; bỏ trống → báo cáo cả khóa học.
        Guid? MaterialId = null
    );
}