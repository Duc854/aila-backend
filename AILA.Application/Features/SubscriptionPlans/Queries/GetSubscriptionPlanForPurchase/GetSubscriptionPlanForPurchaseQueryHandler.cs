using AILA.Application.Common.Interfaces;
using AILA.Application.Features.SubscriptionPlans.Dtos;
using AILA.Application.Features.SubscriptionPlans.Mapping;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.SubscriptionPlans.Queries.GetSubscriptionPlanForPurchase
{
    /// <summary>
    /// UC-09 (Edge case) / AC-92.6 - Gói bị deactivate ở tab admin trong lúc guest đang mở
    /// trang: lượt mua phải bị chặn tại thời điểm khởi tạo mua.
    /// Gói Inactive trả về đúng mã NOT_AVAILABLE nhưng KHÔNG kèm dữ liệu gói, để không rò rỉ
    /// thông tin gói Inactive qua endpoint công khai.
    /// </summary>
    public class GetSubscriptionPlanForPurchaseQueryHandler(IUnitOfWork uow)
        : IRequestHandler<GetSubscriptionPlanForPurchaseQuery, ResponseDto<SubscriptionPlanDto>>
    {
        public async Task<ResponseDto<SubscriptionPlanDto>> Handle(
            GetSubscriptionPlanForPurchaseQuery request,
            CancellationToken ct)
        {
            var plan = await uow.SubscriptionPlans.GetByIdReadOnlyAsync(request.PlanId, ct);

            if (plan is null)
                return ResponseDto<SubscriptionPlanDto>.FailResult(
                    SubscriptionPlanErrors.NotFound,
                    "Không tìm thấy gói đăng ký.");

            if (!plan.IsActive())
                return ResponseDto<SubscriptionPlanDto>.FailResult(
                    SubscriptionPlanErrors.NotAvailable,
                    "Gói đăng ký hiện không còn được bán.");

            return ResponseDto<SubscriptionPlanDto>.SuccessResult(plan.ToPublicDto());
        }
    }
}
