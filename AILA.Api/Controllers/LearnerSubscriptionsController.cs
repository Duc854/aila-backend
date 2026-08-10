using AILA.Api.Extensions;
using AILA.Application.Features.Subscriptions.Dtos;
using AILA.Application.Features.Subscriptions.Queries.GetCurrentSubscription;
using AILA.Application.Features.Subscriptions.Queries.GetSubscriptionResourceUsage;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shared.Wrappers;
using System.Threading;
using System.Threading.Tasks;

namespace AILA.Api.Controllers
{
    /// <summary>
    /// UC-18: Review Current Subscription.
    /// UC-21: Review Subscription Resource Usage.
    /// </summary>
    [ApiController]
    [Route("api/learner/subscriptions")]
    [Authorize(Roles = "Learner")]
    public class LearnerSubscriptionsController : ControllerBase
    {
        private readonly ISender _sender;

        public LearnerSubscriptionsController(ISender sender)
        {
            _sender = sender;
        }

        /// <summary>
        /// UC-18 — Xem thông tin gói đăng ký hiện tại: tên gói, trạng thái, ngày kích hoạt,
        /// ngày hết hạn, số ngày còn lại (BR-01, BR-02).
        /// AF-01: Không có gói Active → trả HasActiveSubscription = false.
        /// </summary>
        [HttpGet("current")]
        [ProducesResponseType(typeof(ResponseDto<CurrentSubscriptionDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseDto<object>), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetCurrentSubscription(CancellationToken ct)
        {
            var identity = HttpContext.GetUserIdentity();
            if (identity == null)
                return Unauthorized(ResponseDto<object>.FailResult("UNAUTHORIZED", "Xác thực thất bại."));

            var result = await _sender.Send(
                new GetCurrentSubscriptionQuery(identity.UserId), ct);

            return Ok(result);
        }

        /// <summary>
        /// UC-21 — Xem hạn mức (allocated quota), lượng đã sử dụng (used quota)
        /// và lượng còn lại (remaining quota) của từng tài nguyên gói đăng ký (BR-01, BR-02, BR-03, AF-01).
        /// </summary>
        [HttpGet("resource-usage")]
        [ProducesResponseType(typeof(ResponseDto<SubscriptionResourceUsageDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseDto<object>), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetResourceUsage(CancellationToken ct)
        {
            var identity = HttpContext.GetUserIdentity();
            if (identity == null)
                return Unauthorized(ResponseDto<object>.FailResult("UNAUTHORIZED", "Xác thực thất bại."));

            var result = await _sender.Send(
                new GetSubscriptionResourceUsageQuery(identity.UserId), ct);

            return Ok(result);
        }
    }
}
