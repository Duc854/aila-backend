using AILA.Application.Common.Interfaces;
using AILA.Application.Features.SubscriptionPlans.Dtos;
using AILA.Application.Features.SubscriptionPlans.Mapping;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.SubscriptionPlans.Queries.GetSubscriptionPlans
{
    public class GetSubscriptionPlansQueryHandler(IUnitOfWork uow)
        : IRequestHandler<GetSubscriptionPlansQuery, ResponseDto<IEnumerable<AdminSubscriptionPlanDto>>>
    {
        public async Task<ResponseDto<IEnumerable<AdminSubscriptionPlanDto>>> Handle(
            GetSubscriptionPlansQuery request,
            CancellationToken ct)
        {
            var plans = await uow.SubscriptionPlans.GetAllOrderedAsync(ct);

            var result = plans.Select(p => p.ToAdminDto()).ToList();

            return ResponseDto<IEnumerable<AdminSubscriptionPlanDto>>.SuccessResult(result);
        }
    }
}
