namespace AILA.Application.Features.Profile.Dtos
{
    public record TagDto(Guid Id, string Name);

    public record LearnerInfoDto(
        string? LearnerType,
        string? KnowledgeLevel,
        bool HasCompletedOnboarding,
        IEnumerable<TagDto> LearningGoals
    );

    public record EnrollmentSummaryDto(
        Guid CourseId,
        string CourseName,
        string? ThumbnailUrl,
        string CategoryName,
        string? Description,
        double DurationHours,
        string Status,
        int ProgressPct,
        int TotalMaterials,
        int CompletedMaterials,
        DateTime EnrolledAt,
        DateTime? CompletedAt,
        DateTime? LastAccessedAt
    );

    public record LearnerProfileDto(
        Guid Id,
        string FullName,
        string Email,
        string? AvatarUrl,
        string Role,
        LearnerInfoDto Learner,
        IEnumerable<EnrollmentSummaryDto> Enrollments
    );
}
