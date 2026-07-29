namespace AILA.Application.Common.Helpers
{
    /// <summary>
    /// Password policy cho luồng Reset Password: chỉ yêu cầu tối thiểu 8 ký tự —
    /// giữ đồng nhất với ChangePassword để cùng một tài khoản không gặp hai chuẩn khác nhau.
    /// </summary>
    public static class PasswordPolicy
    {
        public const int MinLength = 8;

        /// <summary>
        /// Trả về danh sách vi phạm (rỗng nghĩa là hợp lệ). Vẫn trả về danh sách thay vì bool
        /// để AF-03 báo được đầy đủ mọi lỗi trong một lần, kể cả khi policy siết lại sau này.
        /// </summary>
        public static IReadOnlyList<string> Validate(string? password)
        {
            var violations = new List<string>();

            if (string.IsNullOrWhiteSpace(password))
            {
                violations.Add("Mật khẩu mới không được để trống.");
                return violations;
            }

            if (password.Length < MinLength)
                violations.Add($"Mật khẩu phải có ít nhất {MinLength} ký tự.");

            return violations;
        }
    }
}
