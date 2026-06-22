using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AILA.Domain.Entities
{
    public class VideoMaterial
    {
        public Guid MaterialId { get; private set; }

        // Cột này sẽ lưu URL (như link youtube) HOẶC lưu nguyên đoạn mã <iframe src="..."> tùy bạn cấu hình ở FE
        public string VideoUrl { get; private set; }
        public string? ThumbnailUrl { get; private set; }
        public int DurationSeconds { get; private set; }
        public string? Content { get; private set; }
        public string? CaptionsUrl { get; private set; }
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; private set; }

        public virtual Material Material { get; private set; }

        private VideoMaterial() { }

        public VideoMaterial(Guid materialId, string videoUrl, int durationSeconds, string? thumbnailUrl = null, string? content = null, string? captionsUrl = null)
        {
            if (materialId == Guid.Empty) throw new ArgumentException("MaterialId không hợp lệ.");

            ValidateEmbeddableVideo(videoUrl);

            if (durationSeconds < 0) // Để bằng 0 nếu Expert không biết chính xác thời lượng file nhúng
                throw new ArgumentException("Thời lượng video không được là số âm.");

            MaterialId = materialId;
            VideoUrl = videoUrl.Trim();
            DurationSeconds = durationSeconds;
            ThumbnailUrl = thumbnailUrl?.Trim();
            Content = content?.Trim();
            CaptionsUrl = captionsUrl?.Trim();
        }

        public void UpdateDetails(string videoUrl, int durationSeconds, string? thumbnailUrl, string? content, string? captionsUrl)
        {
            ValidateEmbeddableVideo(videoUrl);

            if (durationSeconds < 0)
                throw new ArgumentException("Thời lượng video không được là số âm.");

            VideoUrl = videoUrl.Trim();
            DurationSeconds = durationSeconds;
            ThumbnailUrl = thumbnailUrl?.Trim();
            Content = content?.Trim();
            CaptionsUrl = captionsUrl?.Trim();

            UpdatedAt = DateTime.UtcNow;
        }

        // --- DOMAIN VALIDATION GIAO DIỆN NHÚNG ---
        private static void ValidateEmbeddableVideo(string embedSource)
        {
            if (string.IsNullOrWhiteSpace(embedSource))
                throw new ArgumentException("Nguồn video nhúng không được để trống.");

            // Kiểm tra xem có phải là một URL hợp lệ HOẶC chứa thẻ nhúng <iframe> không
            bool isUrl = Uri.TryCreate(embedSource, UriKind.Absolute, out _);
            bool isIframe = embedSource.Contains("<iframe", StringComparison.OrdinalIgnoreCase);

            if (!isUrl && !isIframe)
            {
                throw new ArgumentException("Nguồn video không hợp lệ. Vui lòng nhập một liên kết URL hoặc mã nhúng <iframe> hợp lệ.");
            }
        }
    }
}
