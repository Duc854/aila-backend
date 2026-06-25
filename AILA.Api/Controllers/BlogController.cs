using AILA.Application.Features.Blogs.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace AILA.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BlogController : ControllerBase
    {
        private readonly IMediator _mediator;

        public BlogController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("top")]
        public async Task<IActionResult> GetTopBlogs([FromQuery] int count = 2)
        {
            var query = new GetTopBlogsQuery { Count = count };
            var result = await _mediator.Send(query);
            return Ok(result);
        }
    }
}
