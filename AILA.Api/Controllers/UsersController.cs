using AILA.Api.Extensions;
using AILA.Application.Features.Auth.Queries.GetCurrentUser;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Wrappers;

namespace AILA.Api.Controllers
{
    [ApiController]
    [Route("api/users")]
    [Authorize]
    public class UsersController(ISender sender) : ControllerBase
    {
        /// <summary>
        /// Lấy thông tin người dùng hiện tại đang đăng nhập.
        /// </summary>
        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser(CancellationToken ct)
        {
            var identity = HttpContext.GetUserIdentity();
            if (identity == null)
                return Unauthorized(ResponseDto<object>.FailResult("UNAUTHORIZED", "Xác thực thất bại."));

            var query = new GetCurrentUserQuery { UserId = identity.UserId };
            var result = await sender.Send(query, ct);

            if (!result.Success)
                return NotFound(result);

            return Ok(result);
        }
    }
}
