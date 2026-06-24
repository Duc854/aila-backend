using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Domain.Entities
{
    public class Expert
    {
        // Id vừa là Khóa chính, vừa là Khóa ngoại liên kết trực tiếp tới User
        public Guid UserId { get; private set; }
        public string? Bio { get; private set; }
        public string? Specialty { get; private set; }
        public int YearsOfExperience { get; private set; }
        public DateTime? UpdatedAt { get; private set; }

        // Navigation Property (Quan hệ 1-1 với User)
        public virtual User User { get; private set; }

        // Constructor phục vụ cho EF Core
        private Expert() { }

        // Constructor chuẩn DDD khi một User kích hoạt vai trò Expert
        public Expert(Guid userId, string? specialty = null, int yearsOfExperience = 0, string? bio = null)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("UserId không được để trống.", nameof(userId));

            if (yearsOfExperience < 0)
                throw new ArgumentException("Số năm kinh nghiệm không được nhỏ hơn 0.", nameof(yearsOfExperience));

            UserId = userId;
            Specialty = specialty?.Trim();
            YearsOfExperience = yearsOfExperience;
            Bio = bio?.Trim();
        }

        // --- CÁC HÀNH VI NGHIỆP VỤ (METHODS) ---

        /// <summary>
        /// Cập nhật hồ sơ chuyên gia (Bio, Chuyên môn, Kinh nghiệm)
        /// </summary>
        public void UpdateProfile(string? bio, string? specialty, int yearsOfExperience)
        {
            if (yearsOfExperience < 0)
                throw new ArgumentException("Số năm kinh nghiệm không được nhỏ hơn 0.", nameof(yearsOfExperience));

            Bio = bio?.Trim();
            Specialty = specialty?.Trim();
            YearsOfExperience = yearsOfExperience;

            UpdatedAt = DateTime.UtcNow;
        }
    }
}
