using AILA.Api.Extensions;
using AILA.Application.Features.Courses.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AILA.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class CoursesController : ControllerBase
    {
        private readonly IMediator _mediator;

        public CoursesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("{courseId:guid}/content-progress")]
        [Authorize(Roles = "Learner")] 
        public async Task<IActionResult> GetCourseContentWithProgress(Guid courseId)
        {
            var identity = HttpContext.GetUserIdentity();
            if (identity == null) return Unauthorized(new { message = "Xác thực người dùng thất bại hoặc mã token không hợp lệ." });

            var query = new GetCourseContentQuery(courseId, identity.UserId);
            var result = await _mediator.Send(query);

            if (result == null)
            {
                return NotFound(new { message = "Không tìm thấy khóa học hoặc bạn chưa đăng ký tham gia khóa học này." });
            }

            return Ok(result);
        }
    }
}
