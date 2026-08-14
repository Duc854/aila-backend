using AILA.Application.Features.SubscriptionPlans.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.SubscriptionPlans.Commands.UpdateSubscriptionPlan
{
    /// <summary>
    /// UC-91 - Update Subscription Plan (Allowed Roles: Admin).
    /// Cố ý không có Name/TierLevel: hai trường này bất biến sau khi tạo (INV-01, BR-01).
    /// DurationInDays sửa được, nhưng chỉ áp dụng cho các lượt mua sau (INV-03, BR-04).
    /// </summary>
    public record UpdateSubscriptionPlanCommand(
        Guid PlanId,
        string? Description,
        decimal Price,
        int DurationInDays,
        int AiTokenLimit,
        int AiPracticeScenarioLimit,
        int ExpertEvaluationLimit,
        int DisplayOrder
    ) : IRequest<ResponseDto<AdminSubscriptionPlanDto>>;
}
