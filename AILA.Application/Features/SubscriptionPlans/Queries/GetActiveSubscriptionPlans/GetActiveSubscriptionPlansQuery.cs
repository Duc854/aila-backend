using AILA.Application.Features.SubscriptionPlans.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.SubscriptionPlans.Queries.GetActiveSubscriptionPlans
{
    /// <summary>
    /// UC-09 - Explore Subscription Plans (Allowed Roles: Public).
    /// </summary>
    public record GetActiveSubscriptionPlansQuery()
        : IRequest<ResponseDto<IEnumerable<SubscriptionPlanDto>>>;
}
