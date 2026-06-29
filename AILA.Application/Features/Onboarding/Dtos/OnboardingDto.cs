namespace AILA.Application.Features.Onboarding.Dtos
{
    public class OnboardingStatusDto
    {
        public bool HasCompletedOnboarding { get; set; }
        public string? LearnerType { get; set; }
        public string? KnowledgeLevel { get; set; }
        public List<Guid> LearningGoalIds { get; set; } = new();
    }
}
