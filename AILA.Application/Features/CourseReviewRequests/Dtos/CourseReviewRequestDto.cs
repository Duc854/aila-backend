namespace AILA.Application.Features.CourseReviewRequests.Dtos;

/// <summary>
/// DTO trả về cho Expert — thông tin request của chính mình.
/// </summary>
public sealed class CourseReviewRequestDto
{
    public Guid Id { get; init; }
    public Guid CourseId { get; init; }
    public string CourseName { get; init; } = string.Empty;
    public bool IsCourseLocked { get; init; }
    public string Reason { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string? ReviewComment { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ReviewedAt { get; init; }
}

/// <summary>
/// DTO trả về cho Admin — đầy đủ thông tin kèm Expert.
/// </summary>
public sealed class CourseReviewRequestAdminDto
{
    public Guid Id { get; init; }
    public Guid CourseId { get; init; }
    public string CourseName { get; init; } = string.Empty;
    public bool IsCourseLocked { get; init; }
    public Guid ExpertId { get; init; }
    public string ExpertName { get; init; } = string.Empty;
    public string ExpertEmail { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string? ReviewComment { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ReviewedAt { get; init; }
}
