using AILA.Application.Common.Dtos;
using AILA.Application.Common.Interfaces;
using MediatR;

namespace AILA.Application.Features.Notifications.Queries
{
    public class GetNotificationListQueryHandler
        : IRequestHandler<GetNotificationListQuery, List<NotificationDto>>
    {
        private readonly IUnitOfWork _uow;

        public GetNotificationListQueryHandler(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<List<NotificationDto>> Handle(
            GetNotificationListQuery request,
            CancellationToken        cancellationToken)
        {
            // Repository đã xử lý filter theo UserId + sắp xếp mới nhất trước
            var notifications = await _uow.Notifications.GetAllByUserIdAsync(request.UserId);

            // Map Domain Entity → DTO (không expose internal entity ra ngoài API)
            return notifications
                .Select(n => new NotificationDto
                {
                    Id          = n.Id,
                    Title       = n.Title,
                    Body        = n.Body,
                    IsRead      = n.IsRead,
                    ReadAt      = n.ReadAt,
                    Type        = n.Type.ToString(),
                    RedirectUrl = n.RedirectUrl,
                    CreatedAt   = n.CreatedAt
                })
                .ToList();
        }
    }
}
