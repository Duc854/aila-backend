namespace Shared.Models
{
    /// <summary>
    /// Cấu hình SMTP cho Email Service (actor phụ của UC-08).
    /// </summary>
    public class SmtpSettings
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 587;

        /// <summary>true = STARTTLS (port 587), false = SSL ngầm (port 465).</summary>
        public bool UseStartTls { get; set; } = true;

        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        public string FromEmail { get; set; } = string.Empty;
        public string FromName { get; set; } = "AILA";

        public int TimeoutSeconds { get; set; } = 15;

        /// <summary>Số lần thử lại khi gửi thất bại (NFR Reliability).</summary>
        public int MaxRetryAttempts { get; set; } = 3;

        /// <summary>Khoảng chờ giữa các lần thử lại, tính bằng giây.</summary>
        public int RetryDelaySeconds { get; set; } = 5;

        /// <summary>
        /// Chưa cấu hình Host/FromEmail thì coi như chưa bật SMTP — hệ thống rơi về
        /// bộ gửi ghi log (dùng cho môi trường dev).
        /// </summary>
        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(Host) && !string.IsNullOrWhiteSpace(FromEmail);
    }
}
