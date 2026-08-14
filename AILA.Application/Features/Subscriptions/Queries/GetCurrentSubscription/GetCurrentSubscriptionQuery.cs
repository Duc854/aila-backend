using AILA.Application.Common.Interfaces;
using AILA.Application.Features.Subscriptions.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Subscriptions.Queries.GetCurrentSubscription
{
    /// <summary>
    /// UC-18: Review Current Subscription.
    /// Trả về thông tin gói đăng ký hiện tại của learner (BR-01, BR-02).
    /// Nếu không có gói Active → trả HasActiveSubscription = false (AF-01).
    /// </summary>
    public record GetCurrentSubscriptionQuery(Guid LearnerId)
        : IRequest<ResponseDto<CurrentSubscriptionDto>>;

    public class GetCurrentSubscriptionQueryHandler(IUnitOfWork uow)
        : IRequestHandler<GetCurrentSubscriptionQuery, ResponseDto<CurrentSubscriptionDto>>
    {
        public async Task<ResponseDto<CurrentSubscriptionDto>> Handle(
            GetCurrentSubscriptionQuery request,
            CancellationToken cancellationToken)
        {
            // BR-01: một learner chỉ có tối đa một gói Active tại một thời điểm.
            // BR-02: lấy thông tin: plan, status, ngày kích hoạt, ngày hết hạn.
            var subscription = await uow.Subscriptions
                .GetActiveSubscriptionByLearnerIdAsync(request.LearnerId, cancellationToken);

            // AF-01: không có gói đang hoạt động → trả trạng thái mặc định.
            if (subscription is null)
            {
                var noSub = new CurrentSubscriptionDto(
                    HasActiveSubscription: false,
                    SubscriptionId: null,
                    SubscriptionPlanName: "Gói mặc định (Miễn phí)",
                    TierLevel: null,
                    Status: "None",
                    ActivatedAt: null,
                    ExpiredAt: null,
                    RemainingDays: 0);

                return ResponseDto<CurrentSubscriptionDto>.SuccessResult(noSub);
            }

            var dto = new CurrentSubscriptionDto(
                HasActiveSubscription: true,
                SubscriptionId: subscription.Id,
                SubscriptionPlanName: subscription.SubscriptionPlan?.Name
                    ?? subscription.PlanSnapshot.TierLevel.ToString(),
                TierLevel: subscription.PlanSnapshot.TierLevel,
                Status: subscription.Status.ToString(),
                ActivatedAt: subscription.ActivatedAt,
                ExpiredAt: subscription.ExpiredAt,
                RemainingDays: subscription.GetRemainingDays());

            return ResponseDto<CurrentSubscriptionDto>.SuccessResult(dto);
        }
    }
}
