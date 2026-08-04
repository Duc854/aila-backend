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
        public string RefreshTokenHash { get; private set; }
        public bool IsRevoked { get; private set; }
        public DateTime ExpiredAt { get; private set; }

        // Navigation Property
        public virtual User User { get; private set; }

        // Constructor phục vụ EF Core
        private UserToken() { }

        // Constructor chuẩn DDD khi cấp mới một cặp Token đăng nhập
        public UserToken(
            Guid userId,
            string refreshTokenHash,
            DateTime expiredAt)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("Mã người dùng không hợp lệ.", nameof(userId));

            if (string.IsNullOrWhiteSpace(refreshTokenHash))
                throw new ArgumentNullException(nameof(refreshTokenHash), "Refresh Token không được để trống.");

            if (expiredAt <= DateTime.UtcNow)
                throw new ArgumentException("Thời gian hết hạn phải lớn hơn thời điểm hiện tại.", nameof(expiredAt));

            Id = Guid.NewGuid();
            UserId = userId;
            RefreshTokenHash = refreshTokenHash;
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
