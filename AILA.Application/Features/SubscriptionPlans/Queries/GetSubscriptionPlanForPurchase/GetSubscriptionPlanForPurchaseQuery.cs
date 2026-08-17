using AILA.Application.Features.SubscriptionPlans.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.SubscriptionPlans.Queries.GetSubscriptionPlanForPurchase
{
    /// <summary>
    /// UC-09 (Edge case) - Kiểm tra lại gói tại thời điểm chuyển sang mua, không tin dữ liệu
    /// đã render ở client. Trả lỗi nếu gói không còn Active.
    /// LearnerId chỉ có khi caller đã đăng nhập — dùng để tính PurchaseAction.
    /// </summary>
    public record GetSubscriptionPlanForPurchaseQuery(Guid PlanId, Guid? LearnerId = null)
        : IRequest<ResponseDto<SubscriptionPlanDto>>;
}
