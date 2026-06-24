using AILA.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Domain.Entities
{
    public class UserToken : BaseEntity
    {
        public Guid UserId { get; private set; }
        public string RefreshToken { get; private set; }
        public string IpAddress { get; private set; }
        public string? UserAgent { get; private set; }
        public bool IsRevoked { get; private set; }
        public DateTime ExpiredAt { get; private set; }

        // Navigation Property
        public virtual User User { get; private set; }

        // Constructor phục vụ EF Core
        private UserToken() { }

        // Constructor chuẩn DDD khi cấp mới một cặp Token đăng nhập
        public UserToken(Guid userId, string refreshToken, string ipAddress, DateTime expiredAt, string? userAgent = null)
        {
            if (userId == Guid.Empty) throw new ArgumentException("UserId không hợp lệ.");
            if (string.IsNullOrWhiteSpace(refreshToken)) throw new ArgumentException("Refresh token không được trống.");
            if (string.IsNullOrWhiteSpace(ipAddress)) throw new ArgumentException("IP Address không được trống.");
            if (expiredAt <= DateTime.UtcNow) throw new ArgumentException("Thời gian hết hạn phải lớn hơn hiện tại.");

            Id = Guid.NewGuid();
            UserId = userId;
            RefreshToken = refreshToken;
            IpAddress = ipAddress;
            UserAgent = userAgent;
            ExpiredAt = expiredAt;
            IsRevoked = false;
        }

        // --- CÁC HÀNH VI NGHIỆP VỤ (METHODS) ---

        /// <summary>
        /// Kiểm tra xem Refresh Token này còn hạn sử dụng và hợp lệ không
        /// </summary>
        public bool IsValid()
        {
            return !IsRevoked && DateTime.UtcNow < ExpiredAt;
        }

        /// <summary>
        /// Thu hồi token (Dùng khi User bấm Đăng xuất hoặc Token bị nghi ngờ rò rỉ)
        /// </summary>
        public void Revoke()
        {
            if (IsRevoked) return;

            IsRevoked = true;
            UpdateTimestamp();
        }
    }
}
