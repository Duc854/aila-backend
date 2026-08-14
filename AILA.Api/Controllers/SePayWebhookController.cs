using AILA.Application.Features.Payments;
using AILA.Application.Features.Payments.Commands.ConfirmPayment;
using AILA.Application.Features.Payments.Dtos;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shared.Wrappers;
using System.Text.Json;

namespace AILA.Api.Controllers
{
    /// <summary>
    /// UC-19 Steps 4–7: Nhận webhook xác nhận thanh toán từ SePay.
    /// Endpoint này KHÔNG yêu cầu JWT (SePay gọi từ server của họ).
    /// Bảo mật bằng HMAC-SHA256 signature trên header "X-SePay-Signature".
    /// </summary>
    [ApiController]
    [Route("api/webhooks/sepay")]
    [AllowAnonymous]
    public class SePayWebhookController : ControllerBase
    {
        private readonly ISender _sender;
        private readonly ILogger<SePayWebhookController> _logger;

        public SePayWebhookController(
            ISender sender,
            ILogger<SePayWebhookController> logger)
        {
            _sender  = sender;
            _logger  = logger;
        }

        /// <summary>
        /// UC-19 Step 4: SePay POST tới đây khi learner đã thanh toán thành công.
        /// Header "X-SePay-Signature": sha256=HMAC-SHA256(rawBody, webhookSecret).
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ResponseDto<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseDto<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> HandleWebhook(CancellationToken ct)
        {
            // 1. Đọc raw body để xác thực chữ ký HMAC-SHA256
            // EnableBuffering đã được gọi ở middleware pipeline (Program.cs)
            using var reader = new System.IO.StreamReader(Request.Body, leaveOpen: true);
            var rawBody      = await reader.ReadToEndAsync(ct);
            Request.Body.Position = 0;

            var signature = Request.Headers["X-SePay-Signature"].FirstOrDefault() ?? string.Empty;
            var timestamp = Request.Headers["X-SePay-Timestamp"].FirstOrDefault() ?? string.Empty;

            // 2. Deserialize payload
            SePayWebhookDto? payload;
            try
            {
                payload = JsonSerializer.Deserialize<SePayWebhookDto>(
                    rawBody,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "SePay webhook: invalid JSON body.");
                return BadRequest(ResponseDto<object>.FailResult(
                    PaymentErrors.PaymentNotFound, "Payload không hợp lệ."));
            }

            if (payload is null)
                return BadRequest(ResponseDto<object>.FailResult(
                    PaymentErrors.PaymentNotFound, "Payload trống."));

            _logger.LogInformation(
                "SePay webhook received. OrderCode={OrderCode}, Amount={Amount}, Signature={Signature}, Timestamp={Timestamp}",
                payload.Code, payload.TransferAmount, signature, timestamp);

            // 3. Gửi command xử lý
            var command = new ConfirmPaymentCommand(rawBody, signature, payload, timestamp);
            var result  = await _sender.Send(command, ct);

            if (!result.Success)
            {
                _logger.LogWarning(
                    "SePay webhook xử lý thất bại. Content={Content}, SePay_Id={Id}, Error={Error}",
                    payload.Content, payload.Id, result.ErrorCode);

                return result.ErrorCode == PaymentErrors.InvalidSignature
                    ? Unauthorized(result)
                    : BadRequest(result);
            }

            // SePay yêu cầu HTTP 200 để ngừng retry
            return Ok(result);
        }
    }
}
