using AILA.Domain.Enums;

namespace AILA.Application.Features.Reports.Dtos;

public record ReportDetailDto(
    Guid Id,
    Guid LearnerId,
    string ReporterName,
    Guid? CourseId,
    string? CourseName,
    Guid? MaterialId,
    string? MaterialTitle,
    ReportType ReportType,
    string? Description,
    ReportStatus Status,
    DateTime CreatedAt,
    DateTime? ResolvedAt
);