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
    /// Theo tài liệu SePay: https://my.sepay.vn/userapi
    /// Field names dùng camelCase để khớp với JsonSerializer khi PropertyNameCaseInsensitive = true.
    /// </summary>
    public record SePayWebhookDto(
        /// <summary>ID giao dịch nội bộ của SePay</summary>
        int Id,
        /// <summary>Ngân hàng nhận tiền (ví dụ: MBBank, Vietcombank)</summary>
        string Gateway,
        /// <summary>Thời điểm giao dịch diễn ra (yyyy-MM-dd HH:mm:ss)</summary>
        string TransactionDate,
        /// <summary>Số tài khoản nhận tiền</summary>
        string AccountNumber,
        /// <summary>Nội dung chuyển khoản — chứa OrderCode của hệ thống</summary>
        string Content,
        /// <summary>Số tiền chuyển (VND)</summary>
        decimal TransferAmount,
        /// <summary>Loại giao dịch: "in" = tiền vào, "out" = tiền ra</summary>
        string TransferType,
        /// <summary>Mã tham chiếu giao dịch từ ngân hàng (transaction code)</summary>
        string? ReferenceCode,
        /// <summary>Code — thường trùng với Content, dùng để tra cứu nội dung đã cài</summary>
        string? Code);
}
