using AILA.Api.Extensions;
using AILA.Application.Features.Courses.Commands;
using AILA.Application.Features.Courses.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shared.Wrappers;
using System.Security.Claims;

namespace AILA.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CoursesController : ControllerBase
    {
        private readonly IMediator _mediator;

        public CoursesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// Lấy danh sách khóa học đã công khai, hỗ trợ tìm kiếm và lọc.
        /// Không cần đăng nhập.
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetCourses(
            [FromQuery] string? keyword,
            [FromQuery] Guid? categoryId,
            [FromQuery] Guid? tagId,
            [FromQuery] string? level,
            [FromQuery] int pageIndex = 0,
            [FromQuery] int pageSize = 12)
        {
            var query = new GetCoursesQuery(keyword, categoryId, tagId, level, pageIndex, pageSize);
            var result = await _mediator.Send(query);
            return Ok(ResponseDto<object>.SuccessResult(result));
        }

        /// Lấy chi tiết một khóa học kèm đầy đủ thông tin: Category, Author, Tags, Modules, Materials.
        /// Không cần đăng nhập.
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCourseDetail(Guid id)
        {
            var result = await _mediator.Send(new GetCourseDetailQuery(id));

            if (result == null)
                return NotFound(ResponseDto<object>.FailResult("COURSE_NOT_FOUND", "Không tìm thấy khóa học."));

            return Ok(ResponseDto<object>.SuccessResult(result));
        }

        /// Tham gia khóa học. Yêu cầu đăng nhập với vai trò Learner.
        [HttpPost("{id}/enroll")]
        [Authorize(Roles = "Learner")]
        public async Task<IActionResult> Enroll(Guid id)
        {
            var identity = HttpContext.GetUserIdentity();
            if (identity == null)
                return Unauthorized(ResponseDto<object>.FailResult("UNAUTHORIZED", "Xác thực người dùng thất bại hoặc mã token không hợp lệ."));

            try
            {
                var command = new EnrollCourseCommand(id, identity.UserId);
                var result = await _mediator.Send(command);
                return Ok(ResponseDto<object>.SuccessResult(result));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ResponseDto<object>.FailResult("ENROLL_FAILED", ex.Message));
            }
        }

        [HttpGet("{courseId}/learning-view")]
        [Authorize(Roles = "Learner")]
        public async Task<IActionResult> GetLearningView(Guid courseId)
        {
            {
                var identity = HttpContext.GetUserIdentity();
                if (identity == null) return Unauthorized(new { message = "Xác thực người dùng thất bại hoặc mã token không hợp lệ." });

                var query = new GetCourseLearningViewQuery(courseId, identity.UserId);
                var result = await _mediator.Send(query);

                if (result == null)
                {
                    return NotFound(new { message = "Không tìm thấy khóa học hoặc bạn chưa đăng ký tham gia khóa học này." });
                }

                return Ok(result);
            }
        }
    }
}
