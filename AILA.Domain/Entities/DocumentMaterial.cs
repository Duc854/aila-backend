using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Domain.Entities
{
    public class DocumentMaterial
    {
        // Khóa chính đồng thời là Khóa ngoại tham chiếu sang bảng Material gốc
        public Guid MaterialId { get; private set; }
        public string Content { get; private set; } // Nội dung bài viết văn bản (Markdown/HTML)
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; private set; }

        // Navigation Property (Quan hệ 1-1 ngược lại với Material)
        public virtual Material Material { get; private set; }

        // Constructor phục vụ EF Core
        private DocumentMaterial() { }

        // Constructor chuẩn DDD khi tạo mới tài liệu học tập
        public DocumentMaterial(Guid materialId, string content)
        {
            if (materialId == Guid.Empty)
                throw new ArgumentException("Mã học liệu không hợp lệ.", nameof(materialId));

            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("Nội dung tài liệu văn bản không được để trống.", nameof(content));

            MaterialId = materialId;
            Content = content.Trim();
        }

        // --- CÁC HÀNH VI NGHIỆP VỤ (METHODS) ---

        /// <summary>
        /// Cập nhật nội dung bài viết 
        /// </summary>
        public void UpdateDetails(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("Nội dung tài liệu văn bản không được để trống.", nameof(content));

            Content = content.Trim();

            UpdatedAt = DateTime.UtcNow;
        }
    }
}
