using AILA.Api.Extensions;
using AILA.Application.Common.Dtos;
using AILA.Application.Features.Courses.Commands;
using AILA.Application.Features.Courses.Queries;
using AILA.Application.Features.Experts.Queries;
using AILA.Application.Features.Profile.Commands.UpdateExpertProfile;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Wrappers;

namespace AILA.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ExpertsController : ControllerBase
    {
        private readonly ISender _sender;

        public ExpertsController(ISender sender)
        {
            _sender = sender;
        }

        /// <summary>
        /// Lấy thông tin profile của Expert đang đăng nhập.
        /// Chỉ Expert mới được gọi endpoint này.
        /// </summary>
        [HttpGet("me/profile")]
        [Authorize(Roles = "Expert")]
        public async Task<IActionResult> GetMyProfile()
        {
            // Lấy UserId từ JWT claim (tương tự CoursesController)
            var identity = HttpContext.GetUserIdentity();
            if (identity is null)
                return Unauthorized(
                    ResponseDto<object>.FailResult(
                        "AUTH_FAILED",
                        "Xác thực người dùng thất bại hoặc mã token không hợp lệ."));

            var query  = new GetExpertProfileQuery(identity.UserId);
            var result = await _sender.Send(query);

            if (result is null)
                return NotFound(
                    ResponseDto<object>.FailResult(
                        "NOT_FOUND",
                        "Không tìm thấy thông tin profile Expert."));

            return Ok(ResponseDto<ExpertProfileDto>.SuccessResult(result));
        }

        [HttpPut("profile")]
        [Authorize(Roles = "Expert")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateExpertProfileRequest request, CancellationToken ct)
        {
            var identity = HttpContext.GetUserIdentity();
            if (identity == null)
                return Unauthorized(ResponseDto<object>.FailResult("UNAUTHORIZED", "Xác thực thất bại."));

            var command = new UpdateExpertProfileCommand(
                identity.UserId,
                request.FullName,
                request.AvatarUrl,
                request.Bio,
                request.Specialty,
                request.YearsOfExperience
            );

            var result = await _sender.Send(command, ct);

            if (!result.Success)
            {
                return result.ErrorCode switch
                {
                    "ACCOUNT_INACTIVE" => StatusCode(StatusCodes.Status403Forbidden, result),
                    _ => BadRequest(result)
                };
            }

            return Ok(result);
        }



       
        /// Lấy danh sách tất cả khóa học của Expert đang đăng nhập (cả draft lẫn published).
        /// Hỗ trợ filter theo keyword và trạng thái, phân trang.
        [HttpGet("me/courses")]
        [Authorize(Roles = "Expert")]
        public async Task<IActionResult> GetMyCourses(
            [FromQuery] string? keyword,
            [FromQuery] bool? isPublished,
            [FromQuery] int pageIndex = 0,
            [FromQuery] int pageSize = 12,
            CancellationToken ct = default)
        {
            var identity = HttpContext.GetUserIdentity();
            if (identity is null)
                return Unauthorized(ResponseDto<object>.FailResult("UNAUTHORIZED", "Xác thực người dùng thất bại."));

            var query = new GetExpertCoursesQuery(identity.UserId, keyword, isPublished, pageIndex, pageSize);
            var result = await _sender.Send(query, ct);
            return Ok(ResponseDto<object>.SuccessResult(result));
        }

     
        /// Tạo mới khóa học (ở trạng thái Draft). Chỉ dành cho Expert.
        [HttpPost("me/courses")]
        [Authorize(Roles = "Expert")]
        public async Task<IActionResult> CreateCourse(
            [FromBody] CreateCourseRequest request,
            CancellationToken ct)
        {
            var identity = HttpContext.GetUserIdentity();
            if (identity is null)
                return Unauthorized(ResponseDto<object>.FailResult("UNAUTHORIZED", "Xác thực người dùng thất bại."));

            try
            {
                var command = new CreateCourseCommand(
                    identity.UserId,
                    request.Name,
                    request.CategoryId,
                    request.Level,
                    request.Description,
                    request.ThumbnailUrl,
                    request.TagIds ?? []);

                var result = await _sender.Send(command, ct);
                return CreatedAtAction(nameof(GetMyCourses), ResponseDto<CourseManageResultDto>.SuccessResult(result));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ResponseDto<object>.FailResult("CREATE_FAILED", ex.Message));
            }
        }

      
        /// Cập nhật thông tin cơ bản của khóa học (tên, mô tả, level, category, tags).
        [HttpPut("me/courses/{courseId}")]
        [Authorize(Roles = "Expert")]
        public async Task<IActionResult> EditCourse(
            Guid courseId,
            [FromBody] EditCourseRequest request,
            CancellationToken ct)
        {
            var identity = HttpContext.GetUserIdentity();
            if (identity is null)
                return Unauthorized(ResponseDto<object>.FailResult("UNAUTHORIZED", "Xác thực người dùng thất bại."));

            try
            {
                var command = new EditCourseCommand(
                    courseId,
                    identity.UserId,
                    request.Name,
                    request.CategoryId,
                    request.Level,
                    request.Description,
                    request.ThumbnailUrl,
                    request.TagIds ?? []);

                var result = await _sender.Send(command, ct);
                return Ok(ResponseDto<CourseManageResultDto>.SuccessResult(result));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    ResponseDto<object>.FailResult("FORBIDDEN", ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ResponseDto<object>.FailResult("EDIT_FAILED", ex.Message));
            }
        }

       
        /// Xuất bản khóa học. Khóa học phải có ít nhất 1 module trước khi publish.
        [HttpPatch("me/courses/{courseId}/publish")]
        [Authorize(Roles = "Expert")]
        public async Task<IActionResult> PublishCourse(Guid courseId, CancellationToken ct)
        {
            var identity = HttpContext.GetUserIdentity();
            if (identity is null)
                return Unauthorized(ResponseDto<object>.FailResult("UNAUTHORIZED", "Xác thực người dùng thất bại."));

            var command = new PublishCourseCommand(courseId, identity.UserId);
            var result = await _sender.Send(command, ct);

            if (!result.Success)
            {
                return result.ErrorCode switch
                {
                    "COURSE_NOT_FOUND" => NotFound(result),
                    "FORBIDDEN"        => StatusCode(StatusCodes.Status403Forbidden, result),
                    _                  => BadRequest(result)
                };
            }

            return Ok(result);
        }

    
        /// Hủy xuất bản khóa học (đưa về trạng thái Draft, ẩn khỏi học viên).
        [HttpPatch("me/courses/{courseId}/unpublish")]
        [Authorize(Roles = "Expert")]
        public async Task<IActionResult> UnpublishCourse(Guid courseId, CancellationToken ct)
        {
            var identity = HttpContext.GetUserIdentity();
            if (identity is null)
                return Unauthorized(ResponseDto<object>.FailResult("UNAUTHORIZED", "Xác thực người dùng thất bại."));

            var command = new UnpublishCourseCommand(courseId, identity.UserId);
            var result = await _sender.Send(command, ct);

            if (!result.Success)
            {
                return result.ErrorCode switch
                {
                    "COURSE_NOT_FOUND" => NotFound(result),
                    "FORBIDDEN"        => StatusCode(StatusCodes.Status403Forbidden, result),
                    _                  => BadRequest(result)
                };
            }

            return Ok(result);
        }
    }

  
    public record UpdateExpertProfileRequest(
        string FullName,
        string? AvatarUrl,
        string? Bio,
        string? Specialty,
        int YearsOfExperience
    );

    public record CreateCourseRequest(
        string Name,
        Guid CategoryId,
        string Level,
        string? Description,
        string? ThumbnailUrl,
        List<Guid>? TagIds
    );

    public record EditCourseRequest(
        string Name,
        Guid CategoryId,
        string Level,
        string? Description,
        string? ThumbnailUrl,
        List<Guid>? TagIds
    );
}
