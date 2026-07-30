using AILA.Application.Features.SubscriptionPlans;
using AILA.Application.Features.SubscriptionPlans.Queries.GetActiveSubscriptionPlans;
using AILA.Application.Features.SubscriptionPlans.Queries.GetSubscriptionPlanForPurchase;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Wrappers;

namespace AILA.Api.Controllers
{
    /// <summary>
    /// UC-09 - Explore Subscription Plans. Endpoint công khai, không yêu cầu authentication.
    /// Chỉ đọc: không có action nào cho phép sửa dữ liệu plan (AC-09.8, BR-03).
    /// </summary>
    [ApiController]
    [Route("api/subscription-plans")]
    [AllowAnonymous]
    public class SubscriptionPlansController : ControllerBase
    {
        private readonly ISender _sender;

        public SubscriptionPlansController(ISender sender)
        {
            _sender = sender;
        }

        /// <summary>
        /// UC-09 - Danh sách gói đang bán, sắp xếp theo DisplayOrder (AC-09.1, AC-09.2).
        /// Không có gói Active nào → trả mảng rỗng để UI hiển thị thông báo "không có gói"
        /// (AC-09.4).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetActivePlans(CancellationToken ct)
        {
            var result = await _sender.Send(new GetActiveSubscriptionPlansQuery(), ct);

            return Ok(result);
        }

        /// <summary>
        /// UC-09 (Edge case) - Trang mua gọi endpoint này để xác nhận lại gói còn Active tại
        /// thời điểm khởi tạo mua, thay vì tin dữ liệu đã render.
        /// </summary>
        [HttpGet("{planId:guid}")]
        public async Task<IActionResult> GetPlanForPurchase(
            [FromRoute] Guid planId,
            CancellationToken ct)
        {
            var result = await _sender.Send(new GetSubscriptionPlanForPurchaseQuery(planId), ct);

            return result.Success ? Ok(result) : MapError(result.ErrorCode, result);
        }

        private IActionResult MapError<T>(string? errorCode, ResponseDto<T> result) => errorCode switch
        {
            SubscriptionPlanErrors.NotFound => NotFound(result),
            // Gói tồn tại nhưng đã ngừng bán: 404 để endpoint công khai không tiết lộ
            // sự tồn tại của gói Inactive (NFR UC-09).
            SubscriptionPlanErrors.NotAvailable => NotFound(result),
            _ => BadRequest(result)
        };
    }
}
