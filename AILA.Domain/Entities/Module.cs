using AILA.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Domain.Entities
{
    public class Module : BaseEntity
    {
        public Guid CourseId { get; private set; }
        public string Title { get; private set; }
        public string? Description { get; private set; }
        public int OrderIndex { get; private set; }
        public bool IsPublished { get; private set; }

        // Navigation Property
        public virtual Course Course { get; private set; }

        // --- CẤU HÌNH QUAN HỆ 1-NHIỀU VỚI MATERIAL ---
        private readonly List<Material> _materials = new();
        public virtual IReadOnlyCollection<Material> Materials => _materials.AsReadOnly();

        // Constructor phục vụ EF Core
        private Module() { }

        // Constructor chuẩn DDD khi Expert thêm một Chương mới vào Khóa học
        public Module(Guid courseId, string title, int orderIndex, string? description = null)
        {
            if (courseId == Guid.Empty)
                throw new ArgumentException("CourseId không hợp lệ.", nameof(courseId));

            if (string.IsNullOrWhiteSpace(title) || title.Length < 5 || title.Length > 255)
                throw new ArgumentException("Tiêu đề chương phải từ 5 đến 255 ký tự.", nameof(title));

            if (orderIndex < 0 || orderIndex > 999)
                throw new ArgumentException("Vị trí sắp xếp (OrderIndex) phải nằm trong khoảng từ 0 đến 999.", nameof(orderIndex));

            Id = Guid.NewGuid();
            CourseId = courseId;
            Title = title.Trim();
            OrderIndex = orderIndex;
            Description = description?.Trim();
            IsPublished = false; // Mặc định tạo mới ở dạng nháp
        }

        // --- CÁC HÀNH VI NGHIỆP VỤ (METHODS) ---

        /// <summary>
        /// Cập nhật thông tin chi tiết của Chương học
        /// </summary>
        public void UpdateInfo(string title, string? description)
        {
            if (string.IsNullOrWhiteSpace(title) || title.Length < 5 || title.Length > 255)
                throw new ArgumentException("Tiêu đề chương phải từ 5 đến 255 ký tự.", nameof(title));

            Title = title.Trim();
            Description = description?.Trim();

            UpdateTimestamp();
        }

        /// <summary>
        /// Thay đổi thứ tự hiển thị của chương trong khóa học
        /// </summary>
        public void ChangeOrder(int newOrderIndex)
        {
            if (newOrderIndex < 0 || newOrderIndex > 999)
                throw new ArgumentException("Vị trí sắp xếp phải nằm trong khoảng từ 0 đến 999.", nameof(newOrderIndex));

            if (OrderIndex == newOrderIndex) return;

            OrderIndex = newOrderIndex;
            UpdateTimestamp();
        }

        /// <summary>
        /// Công khai chương học (Học viên có thể nhìn thấy nếu Khóa học cũng được Publish)
        /// </summary>
        public void Publish()
        {
            if (IsPublished) return;

            IsPublished = true;
            UpdateTimestamp();
        }

        /// <summary>
        /// Tạm ẩn chương học
        /// </summary>
        public void Unpublish()
        {
            if (!IsPublished) return;

            IsPublished = false;
            UpdateTimestamp();
        }
    }
}
