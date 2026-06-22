using AILA.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Domain.Entities
{
    public class Notification
    {
        public Guid Id { get; private set; }
        public Guid UserId { get; private set; }
        public string Title { get; private set; }
        public string Body { get; private set; }
        public bool IsRead { get; private set; }
        public DateTime? ReadAt { get; private set; }
        public NotificationType Type { get; private set; }
        public string? RedirectUrl { get; private set; }
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

        // Navigation Property
        public virtual User User { get; private set; }

        // Constructor phục vụ EF Core
        private Notification() { }

        // Constructor chuẩn DDD được gọi khi hệ thống phát hành một thông báo cá nhân hóa
        public Notification(Guid userId, string title, string body, NotificationType type, string? redirectUrl = null)
        {
            if (userId == Guid.Empty) throw new ArgumentException("UserId không hợp lệ.");

            if (string.IsNullOrWhiteSpace(title) || title.Length > 200)
                throw new ArgumentException("Tiêu đề thông báo không được trống và không quá 200 ký tự.", nameof(title));

            if (string.IsNullOrWhiteSpace(body))
                throw new ArgumentException("Nội dung thông báo không được để trống.", nameof(body));

            Id = Guid.NewGuid();
            UserId = userId;
            Title = title.Trim();
            Body = body.Trim();
            Type = type;
            RedirectUrl = redirectUrl?.Trim();
            IsRead = false;
            ReadAt = null;
        }

        // --- CÁC HÀNH VI NGHIỆP VỤ (METHODS) ---

        /// <summary>
        /// Đánh dấu người dùng đã đọc thông báo này
        /// </summary>
        public void MarkAsRead()
        {
            if (IsRead) return; // Đã đọc rồi thì bỏ qua

            IsRead = true;
            ReadAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Đánh dấu ngược lại thành chưa đọc (Nếu người dùng muốn ghim đọc sau)
        /// </summary>
        public void MarkAsUnread()
        {
            if (!IsRead) return;

            IsRead = false;
            ReadAt = null;
        }
    }
}
