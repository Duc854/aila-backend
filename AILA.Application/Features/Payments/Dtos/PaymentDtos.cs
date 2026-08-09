namespace AILA.Application.Features.Payments.Dtos
{
    /// <summary>
    /// UC-19 Step 2: Thông tin QR/chuyển khoản trả về cho learner sau khi tạo payment.
    /// </summary>
    public record CreatePaymentResultDto(
        Guid PaymentId,
        string OrderCode,
        string PaymentContent,
        decimal Amount,
        string QrCodeUrl,
        string BankAccountNumber,
        string BankAccountName,
        string BankName,
        DateTime ExpiredAt);

    /// <summary>
    /// UC-20 BR-03: Chi tiết một payment transaction.
    /// </summary>
    public record PaymentDetailDto(
        Guid Id,
        string SubscriptionPlanName,
        decimal Amount,
        string Status,
        DateTime? PaidAt,
        DateTime CreatedAt,
        string? TransactionCode,
        string PaymentContent,
        int TierLevel,
        int DurationInDays);

    /// <summary>
    /// UC-20: Item trong danh sách lịch sử thanh toán.
    /// </summary>
    public record PaymentHistoryItemDto(
        Guid Id,
        string SubscriptionPlanName,
        decimal Amount,
        string Status,
        DateTime? PaidAt,
        DateTime CreatedAt);

    /// <summary>
    /// UC-20: Kết quả phân trang lịch sử thanh toán (BR-01, BR-02).
    /// </summary>
    public record PaymentHistoryDto(
        IEnumerable<PaymentHistoryItemDto> Items,
        int TotalCount,
        int Page,
        int PageSize,
        int TotalPages);

    /// <summary>
    /// Thông tin SePay QR/chuyển khoản (được tạo bởi ISePayService).
    /// </summary>
    public record SePayPaymentInfoDto(
        string QrCodeUrl,
        string BankAccountNumber,
        string BankAccountName,
        string BankName);

    /// <summary>
    /// UC-19 Webhook payload từ SePay.
    /// </summary>
    public record SePayWebhookDto(
        string OrderCode,
        string TransactionCode,
        decimal Amount,
        string Content,
        string Status);
}
