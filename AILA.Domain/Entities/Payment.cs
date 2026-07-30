using AILA.Domain.Common;
using AILA.Domain.Enums;
using AILA.Domain.ValueObjects;

namespace AILA.Domain.Entities
{
    public class Payment : BaseEntity
    {
        public Guid LearnerId { get; private set; }

        public Guid SubscriptionPlanId { get; private set; }

        #region Payment Information

        public decimal Amount { get; private set; }

        /// <summary>
        /// Mã đơn hàng nội bộ.
        /// </summary>
        public string OrderCode { get; private set; }

        /// <summary>
        /// Nội dung chuyển khoản.
        /// </summary>
        public string PaymentContent { get; private set; }

        /// <summary>
        /// Mã giao dịch trả về từ ngân hàng / SePay.
        /// </summary>
        public string? TransactionCode { get; private set; }

        public PaymentStatus Status { get; private set; }

        public DateTime? PaidAt { get; private set; }

        /// <summary>
        /// Thời điểm giao dịch hết hiệu lực.
        /// </summary>
        public DateTime ExpiredAt { get; private set; }

        #endregion

        #region Subscription Snapshot

        /// <summary>
        /// Snapshot của Subscription Plan tại thời điểm tạo Payment.
        /// </summary>
        public SubscriptionPlanSnapshot PlanSnapshot { get; private set; } = null!;

        #endregion

        #region Navigation Properties

        public virtual User Learner { get; private set; } = null!;

        public virtual SubscriptionPlan SubscriptionPlan { get; private set; } = null!;

        #endregion

        private Payment()
        {
        }

        public Payment(
            Guid learnerId,
            Guid subscriptionPlanId,
            decimal amount,
            string orderCode,
            string paymentContent,
            DateTime expiredAt,
            SubscriptionPlanSnapshot planSnapshot)
        {
            Validate(
                amount,
                orderCode,
                paymentContent,
                expiredAt,
                planSnapshot);

            Id = Guid.NewGuid();

            LearnerId = learnerId;

            SubscriptionPlanId = subscriptionPlanId;

            Amount = amount;

            OrderCode = orderCode.Trim();

            PaymentContent = paymentContent.Trim();

            ExpiredAt = expiredAt;

            PlanSnapshot = planSnapshot;

            Status = PaymentStatus.Pending;
        }

        public void MarkAsSuccess(string transactionCode)
        {
            if (Status != PaymentStatus.Pending)
                throw new InvalidOperationException(
                    "Chỉ giao dịch đang chờ mới có thể xác nhận thanh toán.");

            if (IsExpired())
                throw new InvalidOperationException(
                    "Giao dịch đã hết hạn.");

            if (string.IsNullOrWhiteSpace(transactionCode))
                throw new ArgumentException(
                    "Mã giao dịch không hợp lệ.",
                    nameof(transactionCode));

            TransactionCode = transactionCode.Trim();

            PaidAt = DateTime.UtcNow;

            Status = PaymentStatus.Success;

            UpdateTimestamp();
        }

        public void MarkAsExpired()
        {
            if (Status != PaymentStatus.Pending)
                throw new InvalidOperationException(
                    "Chỉ giao dịch đang chờ mới có thể chuyển sang trạng thái hết hạn.");

            Status = PaymentStatus.Expired;

            UpdateTimestamp();
        }

        public void Cancel()
        {
            if (Status != PaymentStatus.Pending)
                throw new InvalidOperationException(
                    "Chỉ giao dịch đang chờ mới có thể hủy.");

            Status = PaymentStatus.Cancelled;

            UpdateTimestamp();
        }

        public bool IsPending()
        {
            return Status == PaymentStatus.Pending;
        }

        public bool IsSuccessful()
        {
            return Status == PaymentStatus.Success;
        }

        public bool IsCancelled()
        {
            return Status == PaymentStatus.Cancelled;
        }

        public bool IsExpired()
        {
            return Status == PaymentStatus.Pending
                && DateTime.UtcNow >= ExpiredAt;
        }

        #region Validation

        private static void Validate(
            decimal amount,
            string orderCode,
            string paymentContent,
            DateTime expiredAt,
            SubscriptionPlanSnapshot planSnapshot)
        {
            if (amount <= 0)
                throw new ArgumentException(
                    "Số tiền thanh toán phải lớn hơn 0.",
                    nameof(amount));

            if (string.IsNullOrWhiteSpace(orderCode))
                throw new ArgumentException(
                    "Mã đơn hàng không được để trống.",
                    nameof(orderCode));

            if (string.IsNullOrWhiteSpace(paymentContent))
                throw new ArgumentException(
                    "Nội dung chuyển khoản không được để trống.",
                    nameof(paymentContent));

            if (expiredAt <= DateTime.UtcNow)
                throw new ArgumentException(
                    "Thời gian hết hạn thanh toán không hợp lệ.",
                    nameof(expiredAt));
            if(planSnapshot is null)
                throw new ArgumentException(
                    "Thiếu dữ liệu của gói đăng kí",
                    nameof(planSnapshot));
        }

        #endregion
    }
}