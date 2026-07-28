namespace AILA.Application.Features.Reports.Dtos;

/// <summary>
/// Kết quả trả về sau khi admin lock hoặc unlock một khóa học.
/// </summary>
public sealed class CourseModerationResponseDto
{
    public Guid CourseId { get; init; }
    public string CourseName { get; init; } = string.Empty;
    public bool IsPublished { get; init; }
    public bool IsPublicationLocked { get; init; }
    public string Message { get; init; } = string.Empty;
}
