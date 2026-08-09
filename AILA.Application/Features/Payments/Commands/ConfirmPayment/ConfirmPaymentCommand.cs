using AILA.Application.Features.Payments.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Payments.Commands.ConfirmPayment
{
    /// <summary>
    /// UC-19 Steps 4–6: Nhận webhook từ SePay, xác nhận payment thành công,
    /// áp dụng subscription policy cho learner.
    /// </summary>
    public record ConfirmPaymentCommand(
        string RawBody,
        string Signature,
        SePayWebhookDto Payload
    ) : IRequest<ResponseDto<object>>;
}
