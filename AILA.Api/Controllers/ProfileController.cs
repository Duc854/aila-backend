using AILA.Api.Extensions;
using AILA.Application.Features.Profile.Commands.ChangePassword;
using AILA.Application.Features.Profile.Commands.UploadAvatar;
using AILA.Application.Features.Profile.Queries.GetExpertProfile;
using AILA.Application.Features.Profile.Queries.GetLearnerProfile;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Wrappers;

namespace AILA.Api.Controllers
{
    [ApiController]
    [Route("api/profile")]
    [Authorize]
    public class ProfileController(ISender sender) : ControllerBase
    {
        [HttpGet("learner/me")]
        [Authorize(Roles = "Learner")]
        public async Task<IActionResult> GetLearnerProfile(CancellationToken ct)
        {
            var identity = HttpContext.GetUserIdentity();
            if (identity == null)
                return Unauthorized(ResponseDto<object>.FailResult("UNAUTHORIZED", "Xác thực thất bại."));

            var result = await sender.Send(new GetLearnerProfileQuery(identity.UserId), ct);

            if (!result.Success)
            {
                return result.ErrorCode switch
                {
                    "ACCOUNT_INACTIVE" => StatusCode(StatusCodes.Status403Forbidden, result),
                    _ => BadRequest(result)
                };
            }

            return Ok(result);
        }

        [HttpGet("expert/me")]
        [Authorize(Roles = "Expert")]
        public async Task<IActionResult> GetExpertProfile(CancellationToken ct)
        {
            var identity = HttpContext.GetUserIdentity();
            if (identity == null)
                return Unauthorized(ResponseDto<object>.FailResult("UNAUTHORIZED", "Xác thực thất bại."));

            var result = await sender.Send(new GetExpertProfileQuery(identity.UserId), ct);

            if (!result.Success)
            {
                return result.ErrorCode switch
                {
                    "ACCOUNT_INACTIVE" => StatusCode(StatusCodes.Status403Forbidden, result),
                    _ => BadRequest(result)
                };
            }

            return Ok(result);
        }

        [HttpPost("avatar")]
        [RequestSizeLimit(5 * 1024 * 1024)]
        public async Task<IActionResult> UploadAvatar(IFormFile file, CancellationToken ct)
        {
            var identity = HttpContext.GetUserIdentity();
            if (identity == null)
                return Unauthorized(ResponseDto<object>.FailResult("UNAUTHORIZED", "Xác thực thất bại."));

            if (file == null || file.Length == 0)
                return BadRequest(ResponseDto<object>.FailResult("VALIDATION_ERROR", "File ảnh không được để trống."));

            await using var stream = file.OpenReadStream();
            var command = new UploadAvatarCommand(identity.UserId, stream, file.FileName, file.ContentType, file.Length);
            var result = await sender.Send(command, ct);

            if (!result.Success)
            {
                return result.ErrorCode switch
                {
                    "ACCOUNT_INACTIVE" => StatusCode(StatusCodes.Status403Forbidden, result),
                    "USER_NOT_FOUND" => NotFound(result),
                    _ => BadRequest(result)
                };
            }

            return Ok(result);
        }

        [HttpPut("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
        {
            var identity = HttpContext.GetUserIdentity();
            if (identity == null)
                return Unauthorized(ResponseDto<object>.FailResult("UNAUTHORIZED", "Xác thực thất bại."));

            var command = new ChangePasswordCommand(identity.UserId, request.CurrentPassword, request.NewPassword);
            var result = await sender.Send(command, ct);

            if (!result.Success)
            {
                return result.ErrorCode switch
                {
                    "ACCOUNT_INACTIVE" => StatusCode(StatusCodes.Status403Forbidden, result),
                    _ => BadRequest(result)
                };
            }

            return Ok(result);
        }
    }

    public record ChangePasswordRequest(string? CurrentPassword, string NewPassword);
}
