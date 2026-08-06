using AILA.Domain.Enums;

namespace AILA.Application.Features.Reports.Dtos;

public record ReportListItemDto(
    Guid Id,
    Guid LearnerId,
    Guid? CourseId,
    Guid? MaterialId,
    ReportType ReportType,
    string? Description,
    ReportStatus Status,
    DateTime CreatedAt
);
