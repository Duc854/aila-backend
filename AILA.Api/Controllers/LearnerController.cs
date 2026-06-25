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

        /// <summary>
        /// Đăng nhập Learner — Email + Password.
        /// </summary>
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> LearnerLogin([FromBody] Application.Features.Auth.Commands.LearnerLogin.LearnerLoginCommand command, CancellationToken ct)
        {
            var result = await sender.Send(command, ct);

            if (!result.Success)
                return Unauthorized(result);

            return Ok(result);
        }

        /// <summary>
        /// Lấy trạng thái onboarding của Learner hiện tại.
        /// </summary>
        [HttpGet("onboarding")]
        public async Task<IActionResult> GetOnboardingStatus(CancellationToken ct)
        {
            var identity = HttpContext.GetUserIdentity();
            if (identity == null)
                return Unauthorized(ResponseDto<object>.FailResult("UNAUTHORIZED", "Xác thực thất bại."));

            var query = new Application.Features.Auth.Queries.GetOnboardingStatus.GetOnboardingStatusQuery
            {
                UserId = identity.UserId
            };
            var result = await sender.Send(query, ct);

            if (!result.Success)
                return NotFound(result);

            return Ok(result);
        }

        /// <summary>
        /// Hoàn thành khảo sát onboarding cho Learner.
        /// </summary>
        [HttpPut("onboarding")]
        public async Task<IActionResult> CompleteOnboarding([FromBody] CompleteOnboardingRequest request, CancellationToken ct)
        {
            var identity = HttpContext.GetUserIdentity();
            if (identity == null)
                return Unauthorized(ResponseDto<object>.FailResult("UNAUTHORIZED", "Xác thực thất bại."));

            var command = new Application.Features.Auth.Commands.CompleteOnboarding.CompleteOnboardingCommand
            {
                UserId = identity.UserId,
                LearnerType = request.LearnerType,
                KnowledgeLevel = request.KnowledgeLevel,
                TagIds = request.TagIds
            };
            var result = await sender.Send(command, ct);

            if (!result.Success)
                return BadRequest(result);

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

    public record CompleteOnboardingRequest(
        LearnerType LearnerType,
        KnowledgeLevel KnowledgeLevel,
        List<Guid> TagIds
    );
}
