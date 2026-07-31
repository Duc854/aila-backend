using AILA.Domain.Common;
using AILA.Domain.Enums;
using AILA.Domain.ValueObjects;

namespace AILA.Domain.Entities
{
    public class Subscription : BaseEntity
    {
        public Guid LearnerId { get; private set; }

        public Guid SubscriptionPlanId { get; private set; }

        public Guid PaymentId { get; private set; }

        public SubscriptionStatus Status { get; private set; }

        public DateTime ActivatedAt { get; private set; }

        public DateTime ExpiredAt { get; private set; }

        #region Snapshot Information

        /// <summary>
        /// Snapshot của Subscription Plan tại thời điểm thanh toán thành công.
        /// </summary>
        public SubscriptionPlanSnapshot PlanSnapshot { get; private set; } = null!;

        #endregion

        #region Navigation Properties

        public virtual User Learner { get; private set; } = null!;

        public virtual SubscriptionPlan SubscriptionPlan { get; private set; } = null!;

        public virtual Payment Payment { get; private set; } = null!;

        #endregion

        private Subscription()
        {
        }

        public static Subscription Create(Payment payment)
        {
            Validate(payment);

            return new Subscription
            {
                Id = Guid.NewGuid(),

                LearnerId = payment.LearnerId,

                SubscriptionPlanId = payment.SubscriptionPlanId,

                PaymentId = payment.Id,

                PlanSnapshot = payment.PlanSnapshot,

                ActivatedAt = DateTime.UtcNow,

                ExpiredAt = DateTime.UtcNow.AddDays(
                    payment.PlanSnapshot.DurationInDays),

                Status = SubscriptionStatus.Active
            };
        }

        /// <summary>
        /// Gia hạn gói cùng Tier.
        /// </summary>
        public void Extend()
        {
            if (Status != SubscriptionStatus.Active)
                throw new InvalidOperationException(
                    "Chỉ gói đăng ký đang hoạt động mới có thể được gia hạn.");

            ExpiredAt = ExpiredAt.AddDays(
                PlanSnapshot.DurationInDays);

            UpdateTimestamp();
        }

        /// <summary>
        /// Được thay thế bởi gói có Tier cao hơn.
        /// </summary>
        public void Replace()
        {
            if (Status != SubscriptionStatus.Active)
                throw new InvalidOperationException(
                    "Chỉ gói đăng ký đang hoạt động mới có thể được thay thế.");

            Status = SubscriptionStatus.Replaced;

            UpdateTimestamp();
        }

        public void Expire()
        {
            if (Status != SubscriptionStatus.Active)
                throw new InvalidOperationException(
                    "Chỉ gói đăng ký đang hoạt động mới có thể chuyển sang trạng thái hết hạn.");

            Status = SubscriptionStatus.Expired;

            UpdateTimestamp();
        }

        public void Cancel()
        {
            if (Status == SubscriptionStatus.Cancelled)
                throw new InvalidOperationException(
                    "Gói đăng ký đã bị hủy.");

            Status = SubscriptionStatus.Cancelled;

            UpdateTimestamp();
        }

        public bool IsActive()
        {
            return Status == SubscriptionStatus.Active
                && DateTime.UtcNow <= ExpiredAt;
        }

        public bool IsExpired()
        {
            return DateTime.UtcNow > ExpiredAt;
        }

        public int GetRemainingDays()
        {
            return Math.Max(
                0,
                (ExpiredAt.Date - DateTime.UtcNow.Date).Days);
        }

        #region Validation

        private static void Validate(Payment payment)
        {
            if (payment is null)
                throw new ArgumentException(
                    "Thông tin thanh toán không được để trống.",
                    nameof(payment));

            if (!payment.IsSuccessful())
                throw new InvalidOperationException(
                    "Chỉ có thể tạo gói đăng ký từ giao dịch thanh toán thành công.");
        }

        #endregion
    }
}