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
        public int DurationSeconds { get; private set; }
        public string? Content { get; private set; }
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; private set; }

        public virtual Material Material { get; private set; }

        private VideoMaterial() { }

        public VideoMaterial(Guid materialId, string videoUrl, int durationSeconds, string? content = null)
        {
            if (materialId == Guid.Empty) throw new ArgumentException("Mã học liệu không hợp lệ.");

            ValidateVideoUrl(videoUrl);

            if (durationSeconds < 0) // Để bằng 0 nếu Expert không biết chính xác thời lượng file nhúng
                throw new ArgumentException("Thời lượng video không được là số âm.");

            MaterialId = materialId;
            VideoUrl = videoUrl.Trim();
            DurationSeconds = durationSeconds;
            Content = content?.Trim();
        }

        public void UpdateDetails(string videoUrl, int durationSeconds, string? content)
        {
            ValidateVideoUrl(videoUrl);

            if (durationSeconds < 0)
                throw new ArgumentException("Thời lượng video không được là số âm.");

            VideoUrl = videoUrl.Trim();
            DurationSeconds = durationSeconds;
            Content = content?.Trim();

            UpdatedAt = DateTime.UtcNow;
        }

        // --- DOMAIN VALIDATION GIAO DIỆN NHÚNG ---
        private static void ValidateVideoUrl(string videoUrl)
        {
            if (string.IsNullOrWhiteSpace(videoUrl))
                throw new ArgumentException("Video URL không được để trống.");

            if (!Uri.TryCreate(videoUrl, UriKind.Absolute, out _))
                throw new ArgumentException("Video URL không hợp lệ.");
        }
    }
}
