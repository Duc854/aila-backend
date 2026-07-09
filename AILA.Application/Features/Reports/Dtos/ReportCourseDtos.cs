using AILA.Domain.Enums;

namespace AILA.Application.Features.Reports.Dtos
{
    /// <summary>
    /// Body của yêu cầu báo cáo khóa học (UC-33). Reason bắt buộc (BR-01),
    /// Description tùy chọn (AC-6). Reason nhận giá trị số của enum <see cref="ReportType"/> (1..8).
    /// </summary>
    public record ReportCourseRequest(ReportType Reason, string? Description);

    /// <summary>Kết quả sau khi tạo báo cáo — luôn ở trạng thái Pending, đã vào moderation queue (AC-3/AC-4).</summary>
    public record ReportCourseResponseDto(Guid ReportId, string Status, DateTime CreatedAt);

    /// <summary>Một lý do báo cáo hợp lệ để hiển thị trên form (AC-1).</summary>
    public record ReportReasonDto(int Value, string Name);
}
