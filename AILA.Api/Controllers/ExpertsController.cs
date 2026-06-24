using AILA.Api.Extensions;
using AILA.Application.Common.Dtos;
using AILA.Application.Features.Experts.Queries;
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
    }
}
