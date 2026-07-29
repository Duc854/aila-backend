using AILA.Application.Common.Interfaces;
using AILA.Application.Features.SubscriptionPlans.Dtos;
using AILA.Application.Features.SubscriptionPlans.Mapping;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.SubscriptionPlans.Queries.GetActiveSubscriptionPlans
{
    /// <summary>
    /// UC-09 - Explore Subscription Plans.
    /// AC-09.1/AC-09.3: chỉ trả gói Active — lọc ở tầng dữ liệu.
    /// AC-09.2: sắp xếp tăng dần theo DisplayOrder.
    /// AC-09.4: không có gói Active → danh sách rỗng, không phải lỗi; UI hiển thị
    /// thông báo "không có gói".
    /// </summary>
    public class GetActiveSubscriptionPlansQueryHandler(IUnitOfWork uow)
        : IRequestHandler<GetActiveSubscriptionPlansQuery, ResponseDto<IEnumerable<SubscriptionPlanDto>>>
    {
        public async Task<ResponseDto<IEnumerable<SubscriptionPlanDto>>> Handle(
            GetActiveSubscriptionPlansQuery request,
            CancellationToken ct)
        {
            var plans = await uow.SubscriptionPlans.GetActivePlansOrderedAsync(ct);

            var result = plans.Select(p => p.ToPublicDto()).ToList();

            return ResponseDto<IEnumerable<SubscriptionPlanDto>>.SuccessResult(result);
        }
    }
}
