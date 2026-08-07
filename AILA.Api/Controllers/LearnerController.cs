using AILA.Api.Extensions;
using AILA.Application.Features.Authentication.Commands.LearnerLogin;
using AILA.Application.Features.Onboarding.Commands.CompleteOnboarding;
using AILA.Application.Features.Onboarding.Queries.GetOnboardingStatus;
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

            if (!Enum.TryParse<LearnerType>(request.LearnerType, true, out var learnerType))
            {
                return BadRequest(ResponseDto<object>.FailResult(
                    "INVALID_LEARNER_TYPE",
                    "Loại học viên không hợp lệ."));
            }

            if (!Enum.TryParse<KnowledgeLevel>(request.KnowledgeLevel, true, out var knowledgeLevel))
            {
                return BadRequest(ResponseDto<object>.FailResult(
                    "INVALID_KNOWLEDGE_LEVEL",
                    "Trình độ không hợp lệ."));
            }

            var command = new UpdateLearnerProfileCommand(
                identity.UserId,
                request.FullName,
                request.AvatarUrl,
                learnerType,
                knowledgeLevel,
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
        public async Task<IActionResult> LearnerLogin([FromBody] LearnerLoginCommand command, CancellationToken ct)
        {
            var result = await sender.Send(command, ct);

            if (!result.Success)
                return Unauthorized(result);

            SetRefreshTokenCookie(result.Data!.RefreshToken);
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

            var query = new GetOnboardingStatusQuery
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

            if (!Enum.TryParse(request.LearnerType, true, out LearnerType learnerType))
                return BadRequest(ResponseDto<object>.FailResult("INVALID_LEARNER_TYPE", "Loại học viên không hợp lệ."));

            if (!Enum.TryParse(request.KnowledgeLevel, true, out KnowledgeLevel knowledgeLevel))
                return BadRequest(ResponseDto<object>.FailResult("INVALID_KNOWLEDGE_LEVEL", "Trình độ không hợp lệ."));

            var command = new CompleteOnboardingCommand
            {
                UserId = identity.UserId,
                LearnerType = learnerType,
                KnowledgeLevel = knowledgeLevel,
                TagIds = request.TagIds
            };
            var result = await sender.Send(command, ct);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        private void SetRefreshTokenCookie(string refreshToken)
        {
            var isHttps = Request.IsHttps || Request.Headers["X-Forwarded-Proto"].ToString().Equals("https", StringComparison.OrdinalIgnoreCase);
            var options = new CookieOptions
            {
                HttpOnly = true,
                Secure = isHttps,
                SameSite = isHttps ? SameSiteMode.None : SameSiteMode.Lax,
                Path = "/",
                Expires = DateTimeOffset.UtcNow.AddDays(7),
                MaxAge = TimeSpan.FromDays(7)
            };

            Response.Cookies.Append("refreshToken", refreshToken, options);
        }
    }

    public record UpdateLearnerProfileRequest(
        string FullName,
        string? AvatarUrl,
        string? LearnerType,
        string? KnowledgeLevel,
        Guid[]? LearningGoals
    );

    public record CompleteOnboardingRequest(
        string LearnerType,
        string KnowledgeLevel,
        List<Guid> TagIds
    );
}
