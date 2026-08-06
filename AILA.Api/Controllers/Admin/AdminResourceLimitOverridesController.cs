using AILA.Api.Extensions;
using AILA.Application.Features.ResourceLimitOverrides.Commands.CreateAccountResourceLimitOverride;
using AILA.Application.Features.ResourceLimitOverrides.Commands.DeleteAccountResourceLimitOverride;
using AILA.Application.Features.ResourceLimitOverrides.Commands.UpdateAccountResourceLimitOverride;
using AILA.Application.Features.ResourceLimitOverrides.Queries.GetAccountResourceLimitOverride;
using AILA.Application.Features.ResourceLimitOverrides.Queries.GetOverrideEligibleAccounts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shared.Wrappers;

namespace AILA.Api.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/resource-limit-overrides")]
    [Authorize(Roles = "Admin")]
    public class AdminResourceLimitOverridesController : ControllerBase
    {
        private readonly ISender _sender;


        public AdminResourceLimitOverridesController(
            ISender sender)
        {
            _sender = sender;
        }


        /// <summary>
        /// Lấy danh sách account có thể cấu hình resource limit override.
        /// </summary>
        [HttpGet("accounts")]
        public async Task<IActionResult> GetOverrideEligibleAccounts(
            [FromQuery] string? keyword,
            [FromQuery] int pageIndex = 0,
            [FromQuery] int pageSize = 20)
        {
            var query = new GetOverrideEligibleAccountsQuery(
                keyword,
                new PageRequest
                {
                    PageIndex = pageIndex,
                    PageSize = pageSize
                });


            var result = await _sender.Send(
                query,
                HttpContext.RequestAborted);


            return Ok(result);
        }

        /// <summary>
        /// Lấy thông tin resource limit override của account.
        /// </summary>
        [HttpGet("{accountId:guid}")]
        public async Task<IActionResult> GetAccountOverride(
            Guid accountId)
        {
            var query = new GetAccountResourceLimitOverrideQuery(
                accountId);


            var result = await _sender.Send(
                query,
                HttpContext.RequestAborted);


            return Ok(result);
        }

        /// <summary>
        /// Tạo cấu hình giới hạn tài nguyên riêng cho tài khoản.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateAccountResourceLimitOverride(
            CreateAccountResourceLimitOverrideRequest request,
            CancellationToken cancellationToken)
        {
            var identity = HttpContext.GetUserIdentity()!;


            var command = new CreateAccountResourceLimitOverrideCommand(
                identity.UserId,
                request.AccountId,
                request.AiTokenLimit,
                request.AiPracticeScenarioLimit,
                request.ExpertEvaluationRequestLimit);


            var result = await _sender.Send(
                command,
                cancellationToken);


            if (!result.Success)
            {
                return BadRequest(result);
            }


            return Ok(result);
        }

        /// <summary>
        /// Cập nhật cấu hình giới hạn tài nguyên riêng của tài khoản.
        /// </summary>
        [HttpPut("{accountId:guid}")]
        public async Task<IActionResult> UpdateAccountResourceLimitOverride(
            Guid accountId,
            UpdateAccountResourceLimitOverrideRequest request,
            CancellationToken cancellationToken)
        {
            var identity = HttpContext.GetUserIdentity()!;


            var command = new UpdateAccountResourceLimitOverrideCommand(
                identity.UserId,
                accountId,
                request.AiTokenLimit,
                request.AiPracticeScenarioLimit,
                request.ExpertEvaluationRequestLimit);


            var result = await _sender.Send(
                command,
                cancellationToken);


            if (!result.Success)
            {
                return BadRequest(result);
            }


            return Ok(result);
        }

        /// <summary>
        /// Xóa cấu hình giới hạn tài nguyên riêng của tài khoản.
        /// </summary>
        [HttpDelete("{accountId:guid}")]
        public async Task<IActionResult> DeleteAccountResourceLimitOverride(
            Guid accountId,
            CancellationToken cancellationToken)
        {
            var identity = HttpContext.GetUserIdentity()!;


            var command = new DeleteAccountResourceLimitOverrideCommand(
                identity.UserId,
                accountId);


            var result = await _sender.Send(
                command,
                cancellationToken);


            if (!result.Success)
            {
                return BadRequest(result);
            }


            return Ok(result);
        }
    }
}
