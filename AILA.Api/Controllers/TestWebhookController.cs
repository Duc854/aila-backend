using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AILA.Api.Controllers
{
    /// <summary>
    /// Controller tạm thời để test webhook signature validation
    /// </summary>
    [ApiController]
    [Route("api/test/webhook")]
    [AllowAnonymous]
    public class TestWebhookController : ControllerBase
    {
        private readonly ILogger<TestWebhookController> _logger;
        private readonly IConfiguration _configuration;

        public TestWebhookController(
            ILogger<TestWebhookController> logger,
            IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        [HttpPost("debug")]
        public async Task<IActionResult> DebugWebhook()
        {
            // Đọc raw body
            using var reader = new StreamReader(Request.Body, leaveOpen: true);
            var rawBody = await reader.ReadToEndAsync();
            Request.Body.Position = 0;

            // Lấy headers
            var sePaySignature = Request.Headers["X-SePay-Signature"].FirstOrDefault() ?? "";
            var sePayTimestamp = Request.Headers["X-SePay-Timestamp"].FirstOrDefault() ?? "";

            // Lấy webhook secret từ config
            var webhookSecret = _configuration["SePay:WebhookSecret"] ?? "";

            _logger.LogInformation("=== WEBHOOK DEBUG INFO ===");
            _logger.LogInformation("Raw Body: {RawBody}", rawBody);
            _logger.LogInformation("X-SePay-Signature: {Signature}", sePaySignature);
            _logger.LogInformation("X-SePay-Timestamp: {Timestamp}", sePayTimestamp);
            _logger.LogInformation("Webhook Secret Length: {Length}", webhookSecret.Length);

            // Test multiple signature variations
            var results = new List<object>();

            if (!string.IsNullOrEmpty(webhookSecret))
            {
                // Test 1: Body only
                results.Add(TestSignature("Body only", rawBody, webhookSecret, sePaySignature));

                // Test 2: Timestamp + Body (GitHub style)
                results.Add(TestSignature("Timestamp + Body", sePayTimestamp + rawBody, webhookSecret, sePaySignature));

                // Test 3: Body + Timestamp
                results.Add(TestSignature("Body + Timestamp", rawBody + sePayTimestamp, webhookSecret, sePaySignature));

                // Test 4: Timestamp + "." + Body (Stripe style)
                results.Add(TestSignature("Timestamp.Body", sePayTimestamp + "." + rawBody, webhookSecret, sePaySignature));
            }

            return Ok(new
            {
                rawBody,
                headers = new
                {
                    sePaySignature,
                    sePayTimestamp
                },
                webhookSecretConfigured = !string.IsNullOrEmpty(webhookSecret),
                signatureTests = results
            });
        }

        private object TestSignature(string testName, string payload, string secret, string receivedSignature)
        {
            try
            {
                using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
                var computedBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
                var computedHex = Convert.ToHexString(computedBytes).ToLowerInvariant();

                // Clean received signature
                var cleanReceived = receivedSignature.ToLowerInvariant().Trim();
                if (cleanReceived.StartsWith("sha256="))
                    cleanReceived = cleanReceived.Substring(7);

                return new
                {
                    testName,
                    payload = payload.Length > 100 ? payload.Substring(0, 100) + "..." : payload,
                    computed = computedHex,
                    received = cleanReceived,
                    isMatch = computedHex == cleanReceived
                };
            }
            catch (Exception ex)
            {
                return new
                {
                    testName,
                    error = ex.Message
                };
            }
        }
    }
}