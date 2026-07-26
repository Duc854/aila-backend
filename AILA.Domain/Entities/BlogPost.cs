using AILA.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Domain.Entities
{
    public class BlogPost : BaseEntity
    {
        public string Title { get; private set; } = string.Empty;
        public string Slug { get; private set; } = string.Empty;
        public string Content { get; private set; } = string.Empty;
        public string? ThumbnailUrl { get; private set; }
        public bool IsPublished { get; private set; }
        public DateTime? PublishedAt { get; private set; }
        public int ViewCount { get; private set; } // Phục vụ đo lường Marketing

        // Constructor phục vụ EF Core
        private BlogPost() { }

        // Constructor chuẩn DDD để tạo một bài viết nháp (Draft)
        public BlogPost(string title, string slug, string content, string? thumbnailUrl = null)
        {
            if (string.IsNullOrWhiteSpace(title) || title.Length < 10 || title.Length > 255)
                throw new ArgumentException("Tiêu đề phải từ 10 đến 255 ký tự.", nameof(title));

            if (string.IsNullOrWhiteSpace(slug))
                throw new ArgumentException("Slug không được để trống.", nameof(slug));

            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("Nội dung bài viết không được để trống.", nameof(content));

            Id = Guid.NewGuid();
            Title = title.Trim();
            Slug = slug.Trim().ToLower();
            Content = content;
            ThumbnailUrl = thumbnailUrl;
            IsPublished = false;
            PublishedAt = null;
            ViewCount = 0;
        }

        // --- CÁC HÀNH VI NGHIỆP VỤ (METHODS) ---

        /// <summary>
        /// Chỉnh sửa nội dung bài viết
        /// </summary>
        public void UpdateContent(string title, string slug, string content, string? thumbnailUrl)
        {
            if (string.IsNullOrWhiteSpace(title) || title.Length < 10 || title.Length > 255)
                throw new ArgumentException("Tiêu đề phải từ 10 đến 255 ký tự.", nameof(title));

            if (string.IsNullOrWhiteSpace(slug))
                throw new ArgumentException("Slug không được để trống.", nameof(slug));

            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("Nội dung không được để trống.", nameof(content));

            Title = title.Trim();
            Slug = slug.Trim().ToLower();
            Content = content;
            ThumbnailUrl = thumbnailUrl;
            UpdateTimestamp();
        }

        /// <summary>
        /// Công khai bài viết lên trang Marketing
        /// </summary>
        public void Publish()
        {
            if (IsPublished) return; // Đã publish rồi thì bỏ qua

            IsPublished = true;
            PublishedAt = DateTime.UtcNow; // Tự động ghi nhận thời gian xuất bản chuẩn UTC
            UpdateTimestamp();
        }

        /// <summary>
        /// Hạ bài viết xuống (Chuyển về trạng thái nháp)
        /// </summary>
        public void Unpublish()
        {
            if (!IsPublished) return;

            IsPublished = false;
            UpdateTimestamp();
        }

        /// <summary>
        /// Tăng lượt xem khi có độc giả click vào đọc bài
        /// </summary>
        public void IncrementViewCount()
        {
            ViewCount++;
        }
    }
}
