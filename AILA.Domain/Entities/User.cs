using AILA.Domain.Common;
using AILA.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Domain.Entities
{
    public class User : BaseEntity
    {
        // Các thuộc tính private set để bảo vệ dữ liệu (Encapsulation)
        public string Email { get; private set; }
        public string? PasswordHash { get; private set; }
        public string FullName { get; private set; }
        public string? AvatarUrl { get; private set; }
        public string? GoogleId { get; private set; }
        public UserRole Role { get; private set; }
        public bool IsActive { get; private set; }

        public virtual Learner? Learner { get; private set; }
        public virtual Expert? Expert { get; private set; }

        // Constructor rỗng bắt buộc cho EF Core khi mapping dữ liệu từ DB lên
        private User() { }

        // Constructor chuẩn DDD để tạo mới một User hợp lệ
        public User(string email, string fullName, UserRole role, string? passwordHash = null, string? googleId = null, string? avatarUrl = null)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email không được để trống.", nameof(email));

            if (string.IsNullOrWhiteSpace(fullName))
                throw new ArgumentException("Tên người dùng không được để trống.", nameof(fullName));

            Id = Guid.NewGuid();
            Email = email.ToLower().Trim();
            FullName = fullName.Trim();
            Role = role;
            PasswordHash = passwordHash;
            GoogleId = googleId;
            AvatarUrl = avatarUrl;
            IsActive = true;
        }

        // --- CÁC HÀNH VI (METHODS) THỂ HIỆN NGHIỆP VỤ ---

        public void UpdateProfile(string fullName, string? avatarUrl)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                throw new ArgumentException("Tên không được để trống.", nameof(fullName));

            FullName = fullName.Trim();
            AvatarUrl = avatarUrl;

            UpdateTimestamp(); // Tự động cập nhật thời gian chỉnh sửa
        }

        public void LinkGoogleAccount(string googleId)
        {
            if (string.IsNullOrWhiteSpace(googleId))
                throw new ArgumentException("Google ID không hợp lệ.", nameof(googleId));

            GoogleId = googleId;
            UpdateTimestamp();
        }

        public void UpdatePassword(string newPasswordHash)
        {
            if (string.IsNullOrWhiteSpace(newPasswordHash))
                throw new ArgumentException("Password hash không hợp lệ.", nameof(newPasswordHash));

            PasswordHash = newPasswordHash;
            UpdateTimestamp();
        }

        public void Deactivate()
        {
            IsActive = false;
            UpdateTimestamp();
        }

        public void Activate()
        {
            IsActive = true;
            UpdateTimestamp();
        }
    }
}
