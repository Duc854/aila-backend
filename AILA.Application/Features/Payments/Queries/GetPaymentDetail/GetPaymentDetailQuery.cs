using AILA.Application.Common.Interfaces;
using AILA.Application.Features.Payments.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Payments.Queries.GetPaymentDetail
{
    /// <summary>
    /// UC-20 Steps 5–6: Learner xem chi tiết một giao dịch thanh toán.
    /// BR-01: Chỉ learner sở hữu mới xem được (learnerId dùng làm filter).
    /// BR-03: Hiển thị đủ: plan name, amount, status, paid date, transaction ref, content.
    /// </summary>
    public record GetPaymentDetailQuery(
        Guid PaymentId,
        Guid LearnerId
    ) : IRequest<ResponseDto<PaymentDetailDto>>;

    public class GetPaymentDetailQueryHandler(IUnitOfWork uow)
        : IRequestHandler<GetPaymentDetailQuery, ResponseDto<PaymentDetailDto>>
    {
        public async Task<ResponseDto<PaymentDetailDto>> Handle(
            GetPaymentDetailQuery request,
            CancellationToken cancellationToken)
        {
            // BR-01: filter theo learnerId để đảm bảo chỉ chủ sở hữu xem được
            var payment = await uow.Payments.GetPaymentDetailAsync(
                request.PaymentId,
                request.LearnerId,
                cancellationToken);

            if (payment is null)
                return ResponseDto<PaymentDetailDto>.FailResult(
                    PaymentErrors.NotFound,
                    "Không tìm thấy giao dịch thanh toán.");

            // BR-03: Chi tiết đầy đủ
            var dto = new PaymentDetailDto(
                Id: payment.Id,
                SubscriptionPlanName: payment.SubscriptionPlan?.Name
                    ?? $"Gói Tier {payment.PlanSnapshot.TierLevel}",
                Amount: payment.Amount,
                Status: payment.Status.ToString(),
                PaidAt: payment.PaidAt,
                CreatedAt: payment.CreatedAt,
                TransactionCode: payment.TransactionCode,
                PaymentContent: payment.PaymentContent,
                TierLevel: payment.PlanSnapshot.TierLevel,
                DurationInDays: payment.PlanSnapshot.DurationInDays);

            return ResponseDto<PaymentDetailDto>.SuccessResult(dto);
        }
    }
}
