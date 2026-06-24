using AILA.Api.Extensions;
using AILA.Application.Features.Courses.Queries.GetCourseLearningView;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shared.Wrappers;
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
            var result = await _mediator.Send(query);

            // Kiểm tra trạng thái Success từ Wrapper do Handler trả về
            if (!result.Success)
            {
                // Nếu Handler trả về thất bại (ví dụ: Không tìm thấy khóa học), map trực tiếp sang NotFound với result nguyên bản
                return NotFound(result);
            }

            // Trả về kết quả thành công chứa ResponseDto chuẩn hóa
            return Ok(result);
        }
    }
}

