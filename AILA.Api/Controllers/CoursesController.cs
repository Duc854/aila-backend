using AILA.Api.Extensions;
using AILA.Application.Features.Courses.Commands;
using AILA.Application.Features.Courses.Queries;
using AILA.Application.Features.Courses.Queries.GetCourseLearningView;
using GetCourseLearningViewQuery = AILA.Application.Features.Courses.Queries.GetCourseLearningView.GetCourseLearningViewQuery;
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
        private readonly ISender _sender;

        public CoursesController(ISender sender)
        {
            _sender = sender;
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
            var result = await _sender.Send(query);
            return Ok(ResponseDto<object>.SuccessResult(result));
        }

        /// Lấy chi tiết một khóa học kèm đầy đủ thông tin: Category, Author, Tags, Modules, Materials.
        /// Không cần đăng nhập.
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCourseDetail(Guid id)
        {
            var result = await _sender.Send(new GetCourseDetailQuery(id));

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
                var result = await _sender.Send(command);
                return Ok(ResponseDto<object>.SuccessResult(result));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ResponseDto<object>.FailResult("ENROLL_FAILED", ex.Message));
            }
        }

        /// Kiểm tra học viên đã đăng ký khóa học chưa. Yêu cầu đăng nhập với vai trò Learner.
        [HttpGet("{id}/enrollment")]
        [Authorize(Roles = "Learner")]
        public async Task<IActionResult> CheckEnrollment(Guid id)
        {
            var identity = HttpContext.GetUserIdentity();
            if (identity == null)
                return Unauthorized(ResponseDto<object>.FailResult("UNAUTHORIZED", "Xác thực người dùng thất bại hoặc mã token không hợp lệ."));

            var query = new AILA.Application.Features.Courses.Queries.CheckEnrollmentQuery(id, identity.UserId);
            var result = await _sender.Send(query);
            return Ok(result);
        }

        [HttpGet("{courseId}/learning-view")]
        [Authorize(Roles = "Learner")]
        public async Task<IActionResult> GetLearningView(Guid courseId)
        {
            var identity = HttpContext.GetUserIdentity();
            if (identity == null)
            {
                // Đồng bộ phản hồi Unauthorized qua bản tin FailResult
                return Unauthorized(ResponseDto<object>.FailResult(
                    "UNAUTHORIZED",
                    "Xác thực người dùng thất bại hoặc mã token không hợp lệ."
                ));
            }

            var query = new GetCourseLearningViewQuery(courseId, identity.UserId);
            var result = await _sender.Send(query);

            // Kiểm tra trạng thái Success từ Wrapper do Handler trả về
            if (!result.Success)
            {
                // Nếu Handler trả về thất bại (ví dụ: Không tìm thấy khóa học), map trực tiếp sang NotFound với result nguyên bản
                return NotFound(result);
            }

            // Trả về kết quả thành công chứa ResponseDto chuẩn hóa
            return Ok(result);
        }

        /// <summary>
        /// Lấy danh sách khóa học nổi bật cho trang chủ.
        /// </summary>
        [HttpGet("top")]
        [AllowAnonymous]
        public async Task<IActionResult> GetTopCourses([FromQuery] int count = 5)
        {
            var query = new Application.Features.Courses.Queries.GetTopCourses.GetTopCoursesQuery { Count = count };
            var result = await _sender.Send(query);
            return Ok(result);
        }
    }
}

