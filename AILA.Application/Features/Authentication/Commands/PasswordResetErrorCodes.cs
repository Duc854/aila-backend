namespace AILA.Application.Features.Authentication.Commands
{
    /// <summary>
    /// Mã lỗi dùng chung cho 3 endpoint của luồng Reset Password (UC-08).
    /// Controller map các mã này sang HTTP status.
    /// </summary>
    public static class PasswordResetErrorCodes
    {
        /// <summary>EDGE-01: input rỗng/null.</summary>
        public const string ValidationError = "VALIDATION_ERROR";

        /// <summary>EDGE-03: email sai định dạng.</summary>
        public const string InvalidEmailFormat = "INVALID_EMAIL_FORMAT";

        /// <summary>EDGE-04: vượt ngưỡng rate limit theo email hoặc IP.</summary>
        public const string RateLimitExceeded = "RATE_LIMIT_EXCEEDED";

        /// <summary>AF-02: OTP sai / hết hạn / đã dùng — không tiết lộ lý do cụ thể.</summary>
        public const string InvalidOrExpiredOtp = "INVALID_OR_EXPIRED_OTP";

        /// <summary>AC-8: reset token sai / hết hạn / đã được tiêu thụ.</summary>
        public const string InvalidOrExpiredToken = "INVALID_OR_EXPIRED_TOKEN";

        /// <summary>AF-03: password mới không đạt policy hoặc confirm không khớp.</summary>
        public const string InvalidPassword = "INVALID_PASSWORD";

        /// <summary>EDGE-09: password mới trùng password hiện tại.</summary>
        public const string PasswordReused = "PASSWORD_REUSED";

        /// <summary>EDGE-13: store giữ state reset (Redis) không sẵn sàng.</summary>
        public const string ServiceUnavailable = "SERVICE_UNAVAILABLE";
    }
}
