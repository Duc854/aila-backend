using AILA.Application.Features.SubscriptionPlans.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.SubscriptionPlans.Queries.GetActiveSubscriptionPlans
{
    /// <summary>
    /// UC-09 - Explore Subscription Plans (Allowed Roles: Public).
    /// LearnerId chỉ có khi caller đã đăng nhập — dùng để tính PurchaseAction của từng gói.
    /// </summary>
    public record GetActiveSubscriptionPlansQuery(Guid? LearnerId = null)
        : IRequest<ResponseDto<IEnumerable<SubscriptionPlanDto>>>;
}
