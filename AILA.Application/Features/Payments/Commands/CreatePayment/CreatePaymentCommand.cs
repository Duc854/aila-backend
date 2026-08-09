using AILA.Application.Features.Payments.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Payments.Commands.CreatePayment
{
    /// <summary>
    /// UC-19 Step 1–2: Learner chọn gói và hệ thống tạo payment transaction + QR.
    /// </summary>
    public record CreatePaymentCommand(
        Guid LearnerId,
        Guid SubscriptionPlanId
    ) : IRequest<ResponseDto<CreatePaymentResultDto>>;
}
