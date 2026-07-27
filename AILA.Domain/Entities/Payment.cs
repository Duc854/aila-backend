using AILA.Domain.Common;
using AILA.Domain.Enums;

namespace AILA.Domain.Entities
{
    public class Payment : BaseEntity
    {
        public Guid LearnerId { get; private set; }

        public Guid SubscriptionPlanId { get; private set; }

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
        /// Mã giao dịch trả về từ ngân hàng / Sepay.
        /// </summary>
        public string? TransactionCode { get; private set; }

        public PaymentStatus Status { get; private set; }

        public DateTime? PaidAt { get; private set; }

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
            string paymentContent)
        {
            Validate(
                amount,
                orderCode,
                paymentContent);

            Id = Guid.NewGuid();

            LearnerId = learnerId;

            SubscriptionPlanId = subscriptionPlanId;

            Amount = amount;

            OrderCode = orderCode.Trim();

            PaymentContent = paymentContent.Trim();

            Status = PaymentStatus.Pending;
        }

        public void MarkAsSuccess(string transactionCode)
        {
            if (Status != PaymentStatus.Pending)
                throw new ArgumentException(
                    "Chỉ giao dịch đang chờ mới có thể xác nhận thanh toán.");

            if (string.IsNullOrWhiteSpace(transactionCode))
                throw new ArgumentException(
                    "Mã giao dịch không hợp lệ.",
                    nameof(transactionCode));

            TransactionCode = transactionCode.Trim();

            PaidAt = DateTime.UtcNow;

            Status = PaymentStatus.Success;

            UpdateTimestamp();
        }

        public void MarkAsFailed()
        {
            if (Status != PaymentStatus.Pending)
                throw new ArgumentException(
                    "Chỉ giao dịch đang chờ mới có thể chuyển sang thất bại.");

            Status = PaymentStatus.Failed;

            UpdateTimestamp();
        }

        public void Cancel()
        {
            if (Status != PaymentStatus.Pending)
                throw new ArgumentException(
                    "Chỉ giao dịch đang chờ mới có thể hủy.");

            Status = PaymentStatus.Cancelled;

            UpdateTimestamp();
        }

        public bool IsSuccessful()
        {
            return Status == PaymentStatus.Success;
        }

        public bool IsPending()
        {
            return Status == PaymentStatus.Pending;
        }

        #region Validation

        private static void Validate(
            decimal amount,
            string orderCode,
            string paymentContent)
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
        }

        #endregion
    }
}