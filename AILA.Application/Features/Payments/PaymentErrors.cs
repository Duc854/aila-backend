namespace AILA.Application.Features.Payments
{
    /// <summary>
    /// Mã lỗi dùng chung cho UC-19, UC-20. Controller map sang HTTP status code.
    /// </summary>
    public static class PaymentErrors
    {
        // UC-19 — Purchase
        public const string PlanNotFound        = "PLAN_NOT_FOUND";
        public const string PlanNotAvailable    = "PLAN_NOT_AVAILABLE";
        public const string LowerTierNotAllowed = "LOWER_TIER_NOT_ALLOWED";
        public const string PaymentCreateFailed = "PAYMENT_CREATE_FAILED";

        // UC-19 — Webhook
        public const string InvalidSignature    = "INVALID_WEBHOOK_SIGNATURE";
        public const string PaymentNotFound     = "PAYMENT_NOT_FOUND";
        public const string AlreadyProcessed    = "PAYMENT_ALREADY_PROCESSED";
        public const string PaymentExpired      = "PAYMENT_EXPIRED";
        public const string AmountMismatch      = "PAYMENT_AMOUNT_MISMATCH";

        // UC-20 — History
        public const string Unauthorized        = "UNAUTHORIZED";
        public const string NotFound            = "TRANSACTION_NOT_FOUND";
        public const string InvalidDateRange    = "INVALID_DATE_RANGE";
        public const string InvalidPagination   = "INVALID_PAGINATION";
    }
}
