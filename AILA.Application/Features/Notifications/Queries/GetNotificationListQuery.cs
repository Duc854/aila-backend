using AILA.Application.Common.Dtos;
using MediatR;

namespace AILA.Application.Features.Notifications.Queries
{
    /// <summary>
    /// Query: Lấy toàn bộ danh sách thông báo của user đang đăng nhập.
    /// UserId được lấy từ JWT claim tại Controller.
    /// </summary>
    public record GetNotificationListQuery(Guid UserId)
        : IRequest<List<NotificationDto>>;
}
