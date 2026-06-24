using AILA.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Domain.Entities
{
    public class Category : BaseEntity
    {
        public string Name { get; private set; }
        public string? Description { get; private set; }
        public int OrderIndex { get; private set; }
        public bool IsActive { get; private set; }

        // Constructor phục vụ EF Core
        private Category() { }

        // Constructor chuẩn DDD để Admin tạo mới một Danh mục
        public Category(string name, string? description = null, int orderIndex = 0)
        {
            if (string.IsNullOrWhiteSpace(name) || name.Length < 2 || name.Length > 100)
                throw new ArgumentException("Tên danh mục phải từ 2 đến 100 ký tự.", nameof(name));

            if (orderIndex < 0)
                throw new ArgumentException("Vị trí sắp xếp (OrderIndex) không được nhỏ hơn 0.", nameof(orderIndex));

            Id = Guid.NewGuid();
            Name = name.Trim();
            Description = description?.Trim();
            OrderIndex = orderIndex;
            IsActive = false; // Mặc định tạo xong để ẩn, Admin cấu hình xong mới Active lên
        }

        // --- CÁC HÀNH VI NGHIỆP VỤ (METHODS) ---

        /// <summary>
        /// Chỉnh sửa thông tin cơ bản của danh mục
        /// </summary>
        public void UpdateInfo(string name, string? description)
        {
            if (string.IsNullOrWhiteSpace(name) || name.Length < 2 || name.Length > 100)
                throw new ArgumentException("Tên danh mục phải từ 2 đến 100 ký tự.", nameof(name));

            Name = name.Trim();
            Description = description?.Trim();

            UpdateTimestamp();
        }

        /// <summary>
        /// Thay đổi thứ tự hiển thị của danh mục trên giao diện
        /// </summary>
        public void ChangeOrder(int newOrderIndex)
        {
            if (newOrderIndex < 0)
                throw new ArgumentException("Vị trí sắp xếp không được nhỏ hơn 0.", nameof(newOrderIndex));

            if (OrderIndex == newOrderIndex) return;

            OrderIndex = newOrderIndex;
            UpdateTimestamp();
        }

        /// <summary>
        /// Kích hoạt danh mục hiển thị công khai trên hệ thống
        /// </summary>
        public void Activate()
        {
            if (IsActive) return;

            IsActive = true;
            UpdateTimestamp();
        }

        /// <summary>
        /// Ẩn danh mục khỏi hệ thống khi không dùng đến hoặc đang bảo trì nội dung
        /// </summary>
        public void Deactivate()
        {
            if (!IsActive) return;

            IsActive = false;
            UpdateTimestamp();
        }
    }
}
