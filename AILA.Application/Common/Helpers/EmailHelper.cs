using System.Text.RegularExpressions;

namespace AILA.Application.Common.Helpers
{
    /// <summary>
    /// Tiện ích xử lý email cho luồng Reset Password: chuẩn hoá trước khi băm key
    /// (EDGE-14) và che bớt khi ghi log (NFR Observability).
    /// </summary>
    public static class EmailHelper
    {
        private static readonly Regex EmailPattern = new(
            @"^[^@\s]+@[^@\s.]+(\.[^@\s.]+)+$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary>
        /// EDGE-14: trim khoảng trắng (kể cả Unicode) + lowercase, để cùng một email
        /// luôn sinh ra cùng một Redis key.
        /// </summary>
        public static string Normalize(string? email)
            => (email ?? string.Empty).Trim().ToLowerInvariant();

        /// <summary>EDGE-03: kiểm tra định dạng trước khi chạm tới Redis/DB.</summary>
        public static bool IsValidFormat(string normalizedEmail)
            => !string.IsNullOrWhiteSpace(normalizedEmail)
               && normalizedEmail.Length <= 254
               && EmailPattern.IsMatch(normalizedEmail);

        /// <summary>
        /// Che email cho log: <c>alice@domain.com</c> → <c>a***@domain.com</c>.
        /// Không bao giờ ghi email plaintext vào log.
        /// </summary>
        public static string Mask(string? email)
        {
            var value = Normalize(email);
            if (string.IsNullOrEmpty(value))
                return "(empty)";

            var atIndex = value.IndexOf('@');
            if (atIndex <= 0)
                return "***";

            var localPart = value[..atIndex];
            var domain = value[atIndex..];

            return $"{localPart[0]}***{domain}";
        }
    }
}
