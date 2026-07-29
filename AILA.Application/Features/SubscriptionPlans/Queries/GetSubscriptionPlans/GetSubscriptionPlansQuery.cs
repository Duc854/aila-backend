using AILA.Application.Features.SubscriptionPlans.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.SubscriptionPlans.Queries.GetSubscriptionPlans
{
    /// <summary>
    /// Danh sách gói cho màn quản trị (Allowed Roles: Admin) — gồm cả gói Inactive.
    /// Là màn đích của AC-90.1, AC-91.1/AC-91.9 và AC-92.1/AC-92.8.
    /// </summary>
    public record GetSubscriptionPlansQuery()
        : IRequest<ResponseDto<IEnumerable<AdminSubscriptionPlanDto>>>;
}
