using AILA.Api.Extensions;
using AILA.Application.Common.Dtos;
using AILA.Application.Features.ResourceLimitPolicy.Commands.UpdateDefaultResourceLimitPolicies;
using AILA.Application.Features.ResourceLimitPolicy.Queries.GetDefaultResourceLimitPolicies;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AILA.Api.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/resource-limit-policies")]
    [Authorize(Roles = "Admin")]
    public class AdminResourceLimitPoliciesController : ControllerBase
    {
        private readonly ISender _sender;

        public AdminResourceLimitPoliciesController(ISender sender)
        {
            _sender = sender;
        }


        /// <summary>
        /// API lấy danh sách default resource limit policies hiện tại.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetDefaultResourceLimitPolicies()
        {
            var result = await _sender.Send(
                new GetDefaultResourceLimitPoliciesQuery());

            return Ok(result);
        }


        /// <summary>
        /// API cập nhật default resource limit policies.
        /// </summary>
        [HttpPut]
        public async Task<IActionResult> UpdateDefaultResourceLimitPolicies(
            [FromBody] List<ResourceLimitPolicyDto> policies)
        {
            var identity = HttpContext.GetUserIdentity()!;


            var command = new UpdateDefaultResourceLimitPoliciesCommand(
                policies,
                identity.UserId);


            var result = await _sender.Send(
                command,
                HttpContext.RequestAborted);


            if (!result.Success)
            {
                return BadRequest(result);
            }


            return Ok(result);
        }
    }
}
