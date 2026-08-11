using AILA.Application.Common.Dtos;
using AILA.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AILA.Application.Features.Notifications.Queries
{
    public class GetNotificationListQueryHandler
        : IRequestHandler<GetNotificationListQuery, List<NotificationDto>>
    {
        private readonly IUnitOfWork _uow;
        private readonly ILogger<GetNotificationListQueryHandler> _logger;

        public GetNotificationListQueryHandler(IUnitOfWork uow, ILogger<GetNotificationListQueryHandler> logger)
        {
            _uow = uow;
            _logger = logger;
        }

        public async Task<List<NotificationDto>> Handle(
            GetNotificationListQuery request,
            CancellationToken        cancellationToken)
        {
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi truy vấn danh sách thông báo cho UserId: {UserId}", request.UserId);
                return new List<NotificationDto>();
            }
        }
    }
}

