using AILA.Application.Features.Auth.Commands;
using AILA.Application.Features.Learners.Commands;
using AILA.Application.Features.Learners.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace AILA.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LearnerController : ControllerBase
    {
        private readonly IMediator _mediator;

        public LearnerController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginCommand command)
        {
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileCommand command)
        {
            // In a real application, you would extract the UserId from the authenticated user context (JWT claims)
            // e.g. command.UserId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            await _mediator.Send(command);
            return Ok(new { message = "Cập nhật hồ sơ thành công" });
        }

        [HttpGet("onboarding")]
        public async Task<IActionResult> GetOnboardingStatus([FromQuery] Guid userId)
        {
            // Similar to profile, userId should be extracted from claims.
            var query = new GetOnboardingStatusQuery { UserId = userId };
            var result = await _mediator.Send(query);
            return Ok(new { hasCompletedOnboarding = result });
        }

        [HttpPut("onboarding")]
        public async Task<IActionResult> CompleteOnboarding([FromBody] CompleteOnboardingCommand command)
        {
            // Extract UserId from claims in real implementation
            await _mediator.Send(command);
            return Ok(new { message = "Hoàn thành onboarding" });
        }
    }
}
