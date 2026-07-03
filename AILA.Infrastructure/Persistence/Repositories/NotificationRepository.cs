using AILA.Application.Common.Interfaces.Repositories;
using AILA.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AILA.Infrastructure.Persistence.Repositories
{
    public class NotificationRepository : GenericRepository<Notification>, INotificationRepository
    {
        public NotificationRepository(ApplicationDbContext context) : base(context) { }

        /// <summary>
        /// Lấy toàn bộ thông báo của user, sắp xếp mới nhất lên đầu.
        /// AsNoTracking vì đây là query thuần đọc.
        /// </summary>
        public async Task<List<Notification>> GetAllByUserIdAsync(Guid userId)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .AsNoTracking()
                .ToListAsync();
        }
        public async Task MarkAsReadAsync(Guid notificationId, Guid userId, CancellationToken ct = default)
        {
            // Chỉ cho phép đánh dấu notification của chính user đó
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId, ct);

            if (notification is null) return;

            notification.MarkAsRead();
        }

        public async Task MarkAllAsReadAsync(Guid userId, CancellationToken ct = default)
        {
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync(ct);

            foreach (var n in notifications)
                n.MarkAsRead();
        }
    }
}
