using AILA.Application.Features.SubscriptionPlans;
using AILA.Application.Features.SubscriptionPlans.Commands.ChangeSubscriptionPlanStatus;
using AILA.Application.Features.SubscriptionPlans.Commands.CreateSubscriptionPlan;
using AILA.Application.Features.SubscriptionPlans.Commands.UpdateSubscriptionPlan;
using AILA.Application.Features.SubscriptionPlans.Dtos;
using AILA.Application.Features.SubscriptionPlans.Queries.GetSubscriptionPlans;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shared.Wrappers;

namespace AILA.Api.Controllers.Admin
{
    /// <summary>
    /// UC-90 / UC-91 / UC-92 - Quản trị gói đăng ký.
    /// [Authorize(Roles = "Admin")] phủ toàn controller: người dùng không phải Admin bị từ chối
    /// (AC-90.12, AC-91.11, AC-92.9).
    /// </summary>
    [ApiController]
    [Route("api/admin/subscription-plans")]
    [Authorize(Roles = "Admin")]
    public class AdminSubscriptionPlansController : ControllerBase
    {
        private readonly ISender _sender;

        public AdminSubscriptionPlansController(ISender sender)
        {
            _sender = sender;
        }

        /// <summary>
        /// Danh sách gói cho màn quản trị, gồm cả gói Inactive.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetPlans(CancellationToken ct)
        {
            var result = await _sender.Send(new GetSubscriptionPlansQuery(), ct);

            return Ok(result);
        }

        /// <summary>
        /// UC-90 - Create Subscription Plan.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateSubscriptionPlanRequest request,
            CancellationToken ct)
        {
            if (request == null)
                return BadRequest(ResponseDto<object>.FailResult(
                    SubscriptionPlanErrors.ValidationError,
                    "Nội dung gói đăng ký không được để trống."));

            var result = await _sender.Send(
                new CreateSubscriptionPlanCommand(
                    request.Name,
                    request.Description,
                    request.Price,
                    request.TierLevel,
                    request.DurationInDays,
                    request.AiTokenLimit,
                    request.AiPracticeScenarioLimit,
                    request.ExpertEvaluationLimit,
                    request.DisplayOrder),
                ct);

            return result.Success ? Ok(result) : MapError(result.ErrorCode, result);
        }

        /// <summary>
        /// UC-91 - Update Subscription Plan.
        /// Name và TierLevel không nằm trong request: hai trường này bất biến sau khi tạo
        /// (INV-01, BR-01, AC-91.2).
        /// </summary>
        [HttpPut("{planId:guid}")]
        public async Task<IActionResult> Update(
            [FromRoute] Guid planId,
            [FromBody] UpdateSubscriptionPlanRequest request,
            CancellationToken ct)
        {
            if (request == null)
                return BadRequest(ResponseDto<object>.FailResult(
                    SubscriptionPlanErrors.ValidationError,
                    "Nội dung cập nhật không được để trống."));

            var result = await _sender.Send(
                new UpdateSubscriptionPlanCommand(
                    planId,
                    request.Description,
                    request.Price,
                    request.AiTokenLimit,
                    request.AiPracticeScenarioLimit,
                    request.ExpertEvaluationLimit,
                    request.DisplayOrder),
                ct);

            return result.Success ? Ok(result) : MapError(result.ErrorCode, result);
        }

        /// <summary>
        /// UC-92 - Manage Subscription Plan Status. Client chỉ gọi endpoint này sau khi admin
        /// đã xác nhận ở hộp thoại (BR-04, AC-92.5).
        /// </summary>
        [HttpPatch("{planId:guid}/status")]
        public async Task<IActionResult> ChangeStatus(
            [FromRoute] Guid planId,
            [FromBody] ChangeSubscriptionPlanStatusRequest request,
            CancellationToken ct)
        {
            if (request == null)
                return BadRequest(ResponseDto<object>.FailResult(
                    SubscriptionPlanErrors.ValidationError,
                    "Trạng thái cần đổi không được để trống."));

            var result = await _sender.Send(
                new ChangeSubscriptionPlanStatusCommand(planId, request.IsActive),
                ct);

            return result.Success ? Ok(result) : MapError(result.ErrorCode, result);
        }

        private IActionResult MapError<T>(string? errorCode, ResponseDto<T> result) => errorCode switch
        {
            SubscriptionPlanErrors.NotFound => NotFound(result),
            SubscriptionPlanErrors.NameAlreadyExists => Conflict(result),
            SubscriptionPlanErrors.TierLevelAlreadyExists => Conflict(result),
            SubscriptionPlanErrors.AlreadyActive => Conflict(result),
            SubscriptionPlanErrors.AlreadyInactive => Conflict(result),
            _ => BadRequest(result)
        };
    }
}
