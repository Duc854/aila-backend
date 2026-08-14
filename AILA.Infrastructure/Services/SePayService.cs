using AILA.Application.Common.Interfaces;
using AILA.Application.Features.Payments.Dtos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace AILA.Infrastructure.Services
{
    /// <summary>
    /// Tích hợp cổng thanh toán SePay.
    /// Tài liệu: https://docs.sepay.vn
    ///
    /// Cấu hình trong appsettings.json:
    /// "SePay": {
    ///   "ApiKey":          "...",
    ///   "WebhookSecret":   "...",
    ///   "BankAccountNumber": "...",
    ///   "BankAccountName": "...",
    ///   "BankCode":        "MB" (mã ngân hàng viết tắt theo SePay)
    /// }
    /// </summary>
    public class SePayService : ISePayService
    {
        private readonly string _apiKey;
        private readonly string _webhookSecret;
        private readonly string _bankAccountNumber;
        private readonly string _bankAccountName;
        private readonly string _bankCode;
        private readonly ILogger<SePayService> _logger;

        // Template QR SePay: https://qr.sepay.vn/img?acc=<account>&bank=<bankCode>&amount=<amount>&des=<description>
        private const string QrBaseUrl = "https://qr.sepay.vn/img";

        public SePayService(IConfiguration configuration, ILogger<SePayService> logger)
        {
            var section = configuration.GetSection("SePay");

            _apiKey            = section["ApiKey"]            ?? string.Empty;
            _webhookSecret     = section["WebhookSecret"]     ?? string.Empty;
            _bankAccountNumber = section["BankAccountNumber"] ?? string.Empty;
            _bankAccountName   = section["BankAccountName"]   ?? string.Empty;
            _bankCode          = section["BankCode"]          ?? "MB";
            _logger            = logger;
        }

        /// <inheritdoc/>
        public SePayPaymentInfoDto CreatePaymentInfo(
            string orderCode,
            decimal amount,
            string description)
        {
            // SePay QR URL format theo tài liệu chính thức
            var encodedDesc = Uri.EscapeDataString(description);
            var amountInt   = (long)Math.Round(amount);

            var qrUrl = $"{QrBaseUrl}?acc={_bankAccountNumber}" +
                        $"&bank={_bankCode}" +
                        $"&amount={amountInt}" +
                        $"&des={encodedDesc}" +
                        $"&template=compact";

            return new SePayPaymentInfoDto(
                QrCodeUrl: qrUrl,
                BankAccountNumber: _bankAccountNumber,
                BankAccountName: _bankAccountName,
                BankName: _bankCode);
        }

        /// <inheritdoc/>
        /// <remarks>
        /// SePay gửi header "X-SePay-Signature" = sha256=HMAC-SHA256(rawBody, webhookSecret).
        /// </remarks>
        public bool VerifyWebhookSignature(string rawBody, string receivedSignature)
        {
            if (string.IsNullOrWhiteSpace(_webhookSecret))
            {
                _logger.LogWarning("SePay webhook secret not configured");
                return false; // Chưa cấu hình secret → từ chối an toàn
            }

            if (string.IsNullOrWhiteSpace(receivedSignature))
            {
                _logger.LogWarning("SePay webhook signature is empty");
                return false;
            }

            // SePay gửi signature với prefix "sha256="
            var normalizedReceived = receivedSignature.ToLowerInvariant().Trim();
            if (normalizedReceived.StartsWith("sha256="))
                normalizedReceived = normalizedReceived.Substring(7); // Bỏ "sha256="

            using var hmac       = new HMACSHA256(Encoding.UTF8.GetBytes(_webhookSecret));
            var computedBytes    = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawBody));
            var computedHex      = Convert.ToHexString(computedBytes).ToLowerInvariant();

            var isValid = CryptographicEquals(computedHex, normalizedReceived);
            
            if (!isValid)
            {
                _logger.LogWarning(
                    "SePay webhook signature mismatch. Expected={Expected}, Received={Received}",
                    computedHex, normalizedReceived);
            }

            return isValid;
        }

        /// <summary>
        /// So sánh an toàn tránh timing attack.
        /// </summary>
        private static bool CryptographicEquals(string a, string b)
        {
            if (a.Length != b.Length)
                return false;

            var result = 0;
            for (var i = 0; i < a.Length; i++)
                result |= a[i] ^ b[i];

            return result == 0;
        }
    }
}
