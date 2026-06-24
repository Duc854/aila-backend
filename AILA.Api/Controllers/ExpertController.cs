using AILA.Api.Extensions;
using AILA.Application.Features.Profile.Commands.UpdateExpertProfile;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Wrappers;

namespace AILA.Api.Controllers
{
    [ApiController]
    [Route("api/expert")]
    [Authorize(Roles = "Expert")]
    public class ExpertController(ISender sender) : ControllerBase
    {
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateExpertProfileRequest request, CancellationToken ct)
        {
            var identity = HttpContext.GetUserIdentity();
            if (identity == null)
                return Unauthorized(ResponseDto<object>.FailResult("UNAUTHORIZED", "Xác thực thất bại."));

            var command = new UpdateExpertProfileCommand(
                identity.UserId,
                request.FullName,
                request.AvatarUrl,
                request.Bio,
                request.Specialty,
                request.YearsOfExperience
            );

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

    public record UpdateExpertProfileRequest(
        string FullName,
        string? AvatarUrl,
        string? Bio,
        string? Specialty,
        int YearsOfExperience
    );
}
