using AILA.Application.Features.Users.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AILA.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IMediator _mediator;

        public UsersController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("me")]
        // [Authorize] // Uncomment when JWT authentication middleware is active
        public async Task<IActionResult> GetCurrentUser([FromQuery] Guid userId)
        {
            // In a real app with JWT:
            // var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            // if (Guid.TryParse(idClaim, out Guid parsedId)) userId = parsedId;
            
            var query = new GetCurrentUserQuery { UserId = userId };
            var result = await _mediator.Send(query);
            return Ok(result);
        }
    }
}
