namespace AILA.Application.Common.Dtos
{
    // ── REQUEST ──────────────────────────────────────────────────────────────

    public class AdminLoginRequestDto
    {
        /// <summary>Tài khoản admin (đọc từ appsettings)</summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>Mật khẩu admin (đọc từ appsettings)</summary>
        public string Password { get; set; } = string.Empty;
    }

    public class ExpertLoginRequestDto
    {
        /// <summary>Email đăng ký tài khoản Expert</summary>
        public string Email    { get; set; } = string.Empty;

        /// <summary>Mật khẩu đã đăng ký</summary>
        public string Password { get; set; } = string.Empty;
    }

    // ── RESPONSE ─────────────────────────────────────────────────────────────

    public class LoginResponseDto
    {
        public string AccessToken  { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public string Role         { get; set; } = string.Empty;
        public Guid   UserId       { get; set; }
        public string FullName     { get; set; } = string.Empty;
        public string Email        { get; set; } = string.Empty;
    }
}
