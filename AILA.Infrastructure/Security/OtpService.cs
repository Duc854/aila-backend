using System.Security.Cryptography;
using System.Text;
using AILA.Application.Common.Interfaces;
using Microsoft.Extensions.Options;
using Shared.Models;

namespace AILA.Infrastructure.Security
{
    /// <summary>
    /// Sinh/xác thực OTP và reset token cho UC-08.
    ///
    /// OTP được băm bằng HMAC-SHA256 với khoá bí mật phía server trước khi lưu store.
    /// Băm trần (SHA256 thuần) là không đủ: OTP chỉ có 10^6 khả năng, ai đọc được Redis
    /// sẽ dựng bảng tra trong vài giây. Khoá HMAC khiến việc đó bất khả thi nếu không có
    /// đồng thời cả Redis lẫn cấu hình ứng dụng.
    /// </summary>
    public sealed class OtpService : IOtpService
    {
        private readonly PasswordResetSettings _settings;
        private readonly byte[] _hmacKey;

        public OtpService(IOptions<PasswordResetSettings> settings)
        {
            _settings = settings.Value;

            if (string.IsNullOrWhiteSpace(_settings.OtpHashSecret))
            {
                throw new InvalidOperationException(
                    "PasswordResetSettings:OtpHashSecret chưa được cấu hình. " +
                    "Không thể băm OTP an toàn nếu thiếu khoá này.");
            }

            _hmacKey = Encoding.UTF8.GetBytes(_settings.OtpHashSecret);
        }

        /// <inheritdoc />
        public string GenerateOtp()
        {
            var length = _settings.OtpLength > 0 ? _settings.OtpLength : 6;
            var digits = new char[length];

            for (var i = 0; i < length; i++)
            {
                // RandomNumberGenerator (CSPRNG), không dùng System.Random.
                // GetInt32 lấy mẫu không thiên lệch nên mọi chữ số đều đồng xác suất.
                digits[i] = (char)('0' + RandomNumberGenerator.GetInt32(0, 10));
            }

            return new string(digits);
        }

        /// <inheritdoc />
        public string HashOtp(string normalizedEmail, string otp)
        {
            // Gắn email vào payload để hash của cùng một OTP khác nhau giữa các tài khoản.
            var payload = Encoding.UTF8.GetBytes($"{normalizedEmail}|{otp}");
            var hash = HMACSHA256.HashData(_hmacKey, payload);

            return Convert.ToHexString(hash);
        }

        /// <inheritdoc />
        public bool VerifyOtp(string normalizedEmail, string otp, string expectedHash)
        {
            if (string.IsNullOrEmpty(otp) || string.IsNullOrEmpty(expectedHash))
                return false;

            var actual = Encoding.UTF8.GetBytes(HashOtp(normalizedEmail, otp));
            var expected = Encoding.UTF8.GetBytes(expectedHash);

            // FixedTimeEquals trả false ngay khi độ dài lệch, nên chặn sớm để tránh throw.
            if (actual.Length != expected.Length)
                return false;

            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }

        /// <inheritdoc />
        public string GenerateResetToken()
        {
            // 256 bit entropy, mã hoá base64url để an toàn khi đi qua URL/JSON.
            var bytes = RandomNumberGenerator.GetBytes(32);

            return Convert.ToBase64String(bytes)
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');
        }
    }
}
