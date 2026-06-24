using AILA.Api.Extensions;
using AILA.Application.Common.Dtos;
using AILA.Application.Features.Notifications.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Wrappers;

namespace AILA.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationsController : ControllerBase
    {
        private readonly ISender _sender;

        public NotificationsController(ISender sender)
        {
            _sender = sender;
        }

        /// <summary>
        /// Lấy toàn bộ danh sách thông báo của người dùng đang đăng nhập.
        /// Hỗ trợ cả 3 role: Admin, Expert, Learner.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetMyNotifications()
        {
            var identity = HttpContext.GetUserIdentity();
            if (identity is null)
                return Unauthorized(
                    ResponseDto<object>.FailResult(
                        "AUTH_FAILED",
                        "Xác thực người dùng thất bại hoặc mã token không hợp lệ."));

            var query  = new GetNotificationListQuery(identity.UserId);
            var result = await _sender.Send(query);

            return Ok(ResponseDto<List<NotificationDto>>.SuccessResult(result));
        }
    }
}
