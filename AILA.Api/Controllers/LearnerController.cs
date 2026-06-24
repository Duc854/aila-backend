using AILA.Api.Extensions;
using AILA.Application.Features.Profile.Commands.UpdateLearnerProfile;
using AILA.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Wrappers;

namespace AILA.Api.Controllers
{
    [ApiController]
    [Route("api/learner")]
    [Authorize(Roles = "Learner")]
    public class LearnerController(ISender sender) : ControllerBase
    {
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateLearnerProfileRequest request, CancellationToken ct)
        {
            var identity = HttpContext.GetUserIdentity();
            if (identity == null)
                return Unauthorized(ResponseDto<object>.FailResult("UNAUTHORIZED", "Xác thực thất bại."));

            var command = new UpdateLearnerProfileCommand(
                identity.UserId,
                request.FullName,
                request.AvatarUrl,
                request.LearnerType,
                request.KnowledgeLevel,
                request.LearningGoals ?? []
            );

            var result = await sender.Send(command, ct);

            if (!result.Success)
            {
                return result.ErrorCode switch
                {
                    "ACCOUNT_INACTIVE" => StatusCode(StatusCodes.Status403Forbidden, result),
                    "UNPUBLISHED_TAG" => UnprocessableEntity(result),
                    _ => BadRequest(result)
                };
            }

            return Ok(result);
        }
    }

    public record UpdateLearnerProfileRequest(
        string FullName,
        string? AvatarUrl,
        LearnerType? LearnerType,
        KnowledgeLevel? KnowledgeLevel,
        Guid[]? LearningGoals
    );
}
