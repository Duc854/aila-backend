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
        public string? DocumentUrl { get; private set; } // Link file tài liệu ngoại vi (Google Drive, PDF...)
        public int? FileSizeKb { get; private set; }
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; private set; }

        // Navigation Property (Quan hệ 1-1 ngược lại với Material)
        public virtual Material Material { get; private set; }

        // Constructor phục vụ EF Core
        private DocumentMaterial() { }

        // Constructor chuẩn DDD khi tạo mới tài liệu học tập
        public DocumentMaterial(Guid materialId, string content, string? documentUrl = null, int? fileSizeKb = null)
        {
            if (materialId == Guid.Empty)
                throw new ArgumentException("MaterialId không hợp lệ.", nameof(materialId));

            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("Nội dung tài liệu văn bản không được để trống.", nameof(content));

            if (fileSizeKb.HasValue && fileSizeKb.Value <= 0)
                throw new ArgumentException("Dung lượng file (file_size_kb) phải lớn hơn 0.", nameof(fileSizeKb));

            MaterialId = materialId;
            Content = content.Trim();
            DocumentUrl = documentUrl?.Trim();
            FileSizeKb = fileSizeKb;
        }

        // --- CÁC HÀNH VI NGHIỆP VỤ (METHODS) ---

        /// <summary>
        /// Cập nhật nội dung bài viết hoặc file đính kèm của tài liệu
        /// </summary>
        public void UpdateDetails(string content, string? documentUrl, int? fileSizeKb)
        {
            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("Nội dung tài liệu văn bản không được để trống.", nameof(content));

            if (fileSizeKb.HasValue && fileSizeKb.Value <= 0)
                throw new ArgumentException("Dung lượng file phải lớn hơn 0.", nameof(fileSizeKb));

            Content = content.Trim();
            DocumentUrl = documentUrl?.Trim();
            FileSizeKb = fileSizeKb;

            UpdatedAt = DateTime.UtcNow;
        }
    }
}
