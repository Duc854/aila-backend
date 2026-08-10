using AILA.Api.Extensions;
using AILA.Application.Features.Payments;
using AILA.Application.Features.Payments.Commands.CreatePayment;
using AILA.Application.Features.Payments.Dtos;
using AILA.Application.Features.Payments.Queries.GetPaymentDetail;
using AILA.Application.Features.Payments.Queries.GetPaymentHistory;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shared.Wrappers;

namespace AILA.Api.Controllers
{
    /// <summary>
    /// UC-19: Purchase Subscription Plan.
    /// UC-20: Review Payment History.
    /// </summary>
    [ApiController]
    [Route("api/learner/payments")]
    [Authorize(Roles = "Learner")]
    public class LearnerPaymentsController : ControllerBase
    {
        private readonly ISender _sender;

        public LearnerPaymentsController(ISender sender)
        {
            _sender = sender;
        }

        /// <summary>
        /// UC-19 Steps 1–2: Chọn gói và tạo giao dịch thanh toán. Hệ thống trả về
        /// thông tin QR SePay để learner quét và hoàn tất thanh toán.
        /// AF-01: Plan không khả dụng hoặc tier thấp hơn → trả lỗi tương ứng.
        /// BR-01: Chỉ một gói Active tại một thời điểm.
        /// BR-02: Snapshot plan ghi lại tại thời điểm tạo payment.
        /// BR-05: Không mua gói tier thấp hơn gói đang Active.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ResponseDto<CreatePaymentResultDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ResponseDto<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseDto<object>), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> CreatePayment(
            [FromBody] CreatePaymentRequest request,
            CancellationToken ct)
        {
            var identity = HttpContext.GetUserIdentity();
            if (identity == null)
                return Unauthorized(ResponseDto<object>.FailResult("UNAUTHORIZED", "Xác thực thất bại."));

            var command = new CreatePaymentCommand(
                LearnerId: identity.UserId,
                SubscriptionPlanId: request.SubscriptionPlanId);

            var result = await _sender.Send(command, ct);

            return result.Success
                ? StatusCode(StatusCodes.Status201Created, result)
                : MapError(result.ErrorCode, result);
        }

        /// <summary>
        /// UC-20 Steps 1–4: Xem lịch sử thanh toán, hỗ trợ lọc theo ngày và phân trang.
        /// BR-01: Chỉ xem lịch sử của chính mình.
        /// BR-02: Lọc theo fromDate / toDate.
        /// AF-01: Không có giao dịch phù hợp → trả mảng rỗng.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ResponseDto<PaymentHistoryDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseDto<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseDto<object>), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetPaymentHistory(
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            CancellationToken ct = default)
        {
            var identity = HttpContext.GetUserIdentity();
            if (identity == null)
                return Unauthorized(ResponseDto<object>.FailResult("UNAUTHORIZED", "Xác thực thất bại."));

            var query = new GetPaymentHistoryQuery(
                LearnerId: identity.UserId,
                FromDate: fromDate,
                ToDate: toDate,
                Page: page,
                PageSize: pageSize);

            var result = await _sender.Send(query, ct);

            return result.Success ? Ok(result) : MapError(result.ErrorCode, result);
        }

        /// <summary>
        /// UC-20 Steps 5–6: Xem chi tiết một giao dịch thanh toán.
        /// BR-01: Chỉ learner sở hữu mới xem được.
        /// BR-03: Trả đủ: plan name, amount, status, paid date, transaction ref, content.
        /// </summary>
        [HttpGet("{paymentId:guid}")]
        [ProducesResponseType(typeof(ResponseDto<PaymentDetailDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseDto<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ResponseDto<object>), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetPaymentDetail(
            [FromRoute] Guid paymentId,
            CancellationToken ct)
        {
            var identity = HttpContext.GetUserIdentity();
            if (identity == null)
                return Unauthorized(ResponseDto<object>.FailResult("UNAUTHORIZED", "Xác thực thất bại."));

            var query = new GetPaymentDetailQuery(
                PaymentId: paymentId,
                LearnerId: identity.UserId);

            var result = await _sender.Send(query, ct);

            return result.Success ? Ok(result) : MapError(result.ErrorCode, result);
        }

        private IActionResult MapError<T>(string? errorCode, ResponseDto<T> result) =>
            errorCode switch
            {
                PaymentErrors.PlanNotFound        => NotFound(result),
                PaymentErrors.PlanNotAvailable    => NotFound(result),
                PaymentErrors.LowerTierNotAllowed => BadRequest(result),
                PaymentErrors.NotFound            => NotFound(result),
                PaymentErrors.InvalidDateRange    => BadRequest(result),
                PaymentErrors.InvalidPagination   => BadRequest(result),
                _                                 => BadRequest(result)
            };
    }

    /// <summary>Request body cho UC-19 Step 1.</summary>
    public record CreatePaymentRequest(Guid SubscriptionPlanId);
}
