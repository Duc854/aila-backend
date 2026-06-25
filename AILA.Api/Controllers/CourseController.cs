using AILA.Application.Features.Courses.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace AILA.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CourseController : ControllerBase
    {
        private readonly IMediator _mediator;

        public CourseController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("top")]
        public async Task<IActionResult> GetTopCourses([FromQuery] int count = 5)
        {
            var query = new GetTopCoursesQuery { Count = count };
            var result = await _mediator.Send(query);
            return Ok(result);
        }
    }
}
