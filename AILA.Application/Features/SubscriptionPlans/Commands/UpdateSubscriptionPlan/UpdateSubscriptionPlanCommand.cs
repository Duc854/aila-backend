using AILA.Application.Features.SubscriptionPlans.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.SubscriptionPlans.Commands.UpdateSubscriptionPlan
{
    /// <summary>
    /// UC-91 - Update Subscription Plan (Allowed Roles: Admin).
    /// Cố ý không có Name/TierLevel (INV-01, BR-01) và không có DurationInDays vì
    /// <c>SubscriptionPlan.Update()</c> không nhận trường này.
    /// </summary>
    public record UpdateSubscriptionPlanCommand(
        Guid PlanId,
        string? Description,
        decimal Price,
        int AiTokenLimit,
        int AiPracticeScenarioLimit,
        int ExpertEvaluationLimit,
        int DisplayOrder
    ) : IRequest<ResponseDto<AdminSubscriptionPlanDto>>;
}
