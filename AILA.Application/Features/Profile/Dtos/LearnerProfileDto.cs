namespace AILA.Application.Features.Profile.Dtos
{
    public record TagDto(Guid Id, string Name);

    public record LearnerInfoDto(
        string? LearnerType,
        string? KnowledgeLevel,
        bool HasCompletedOnboarding,
        IEnumerable<TagDto> LearningGoals
    );

    public record LearnerProfileDto(
        Guid Id,
        string FullName,
        string Email,
        string? AvatarUrl,
        string Role,
        LearnerInfoDto Learner
    );
}
