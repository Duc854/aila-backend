using AILA.Api.Extensions;
using AILA.Application.Features.Subscriptions.Dtos;
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
    /// UC-21: Review Subscription Resource Usage.
    /// Cho phép Learner xem mức độ sử dụng tài nguyên của gói đăng ký hiện tại.
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

            var query = new GetSubscriptionResourceUsageQuery(identity.UserId);
            var result = await _sender.Send(query, ct);

            return Ok(result);
        }
    }
}
