using AILA.Application.Features.Auth.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace AILA.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AuthController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterCommand command)
        {
            var userId = await _mediator.Send(command);
            return CreatedAtAction(nameof(Register), new { id = userId }, new { message = "Đăng ký thành công", userId });
        }

        [HttpPost("learner/google")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginCommand command)
        {
            var response = await _mediator.Send(command);
            return Ok(response);
        }
    }
}
