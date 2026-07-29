using AILA.Application.Features.SubscriptionPlans.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.SubscriptionPlans.Commands.CreateSubscriptionPlan
{
    /// <summary>
    /// UC-90 - Create Subscription Plan (Allowed Roles: Admin).
    /// Status không nằm trong input: entity luôn gán Active (BR-05).
    /// </summary>
    public record CreateSubscriptionPlanCommand(
        string Name,
        string? Description,
        decimal Price,
        int TierLevel,
        int DurationInDays,
        int AiTokenLimit,
        int AiPracticeScenarioLimit,
        int ExpertEvaluationLimit,
        int DisplayOrder
    ) : IRequest<ResponseDto<AdminSubscriptionPlanDto>>;
}
