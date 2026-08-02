namespace Shared.Models
{
    /// <summary>
    /// Cấu hình luồng Reset Password (UC-08).
    /// Toàn bộ TTL / độ dài OTP / ngưỡng attempt / rate limit đều nằm ở đây
    /// (Options pattern) thay vì hardcode trong handler.
    /// </summary>
    public class PasswordResetSettings
    {
        /// <summary>Prefix cho mọi Redis key của luồng này.</summary>
        public string RedisKeyPrefix { get; set; } = "pwdreset:";

        /// <summary>Số chữ số của OTP (BR-01).</summary>
        public int OtpLength { get; set; } = 6;

        /// <summary>TTL của OTP, tính bằng giây — Redis EXPIRE (BR-01: 5 phút).</summary>
        public int OtpTtlSeconds { get; set; } = 300;

        /// <summary>TTL của reset token, tính bằng giây (NFR: ≤ 10 phút).</summary>
        public int ResetTokenTtlSeconds { get; set; } = 600;

        /// <summary>Số lần verify sai tối đa trên một OTP trước khi huỷ OTP (EDGE-06).</summary>
        public int MaxVerifyAttempts { get; set; } = 5;

        /// <summary>Số lần xin OTP tối đa cho một email trong một cửa sổ rate limit.</summary>
        public int MaxOtpRequestsPerEmail { get; set; } = 5;

        /// <summary>Số lần xin OTP tối đa cho một IP trong một cửa sổ rate limit.</summary>
        public int MaxOtpRequestsPerIp { get; set; } = 20;

        /// <summary>Độ dài cửa sổ rate limit, tính bằng giây (mặc định 1 giờ).</summary>
        public int RateLimitWindowSeconds { get; set; } = 3600;

        /// <summary>
        /// Thời gian tối thiểu (ms) của endpoint request-OTP. Response được "đệm" cho đủ
        /// khoảng này để thời gian phản hồi đồng nhất giữa email tồn tại / không tồn tại
        /// (chống enumeration qua timing — NFR Security).
        /// </summary>
        public int MinRequestDurationMs { get; set; } = 350;

        /// <summary>EDGE-09: từ chối password mới trùng password hiện tại.</summary>
        public bool RejectPasswordSameAsCurrent { get; set; } = true;

        /// <summary>
        /// Khoá HMAC dùng để băm OTP trước khi lưu Redis. Bắt buộc cấu hình —
        /// nếu bỏ trống, kẻ đọc được Redis có thể brute-force 10^6 OTP trong tích tắc.
        /// </summary>
        public string OtpHashSecret { get; set; } = string.Empty;
    }
}
