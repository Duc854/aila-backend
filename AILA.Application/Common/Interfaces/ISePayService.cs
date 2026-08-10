using AILA.Application.Features.Payments.Dtos;

namespace AILA.Application.Common.Interfaces
{
    /// <summary>
    /// Service giao tiếp với cổng thanh toán SePay để tạo link/QR và xác thực webhook.
    /// </summary>
    public interface ISePayService
    {
        /// <summary>
        /// Tạo thông tin thanh toán (QR content, banking info) từ orderCode và amount.
        /// </summary>
        SePayPaymentInfoDto CreatePaymentInfo(
            string orderCode,
            decimal amount,
            string description);

        /// <summary>
        /// Xác thực chữ ký webhook từ SePay.
        /// </summary>
        bool VerifyWebhookSignature(string rawBody, string receivedSignature);
    }
}
