using AILA.Domain.Common;
using AILA.Domain.Enums;

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
        /// Snapshot Tier Level tại thời điểm mua.
        /// </summary>
        public int TierLevel { get; private set; }

        /// <summary>
        /// Snapshot thời hạn gói.
        /// </summary>
        public int DurationInDays { get; private set; }

        /// <summary>
        /// Snapshot giới hạn AI Token.
        /// </summary>
        public int AiTokenLimit { get; private set; }

        /// <summary>
        /// Snapshot giới hạn AI Practice Scenario.
        /// </summary>
        public int AiPracticeScenarioLimit { get; private set; }

        /// <summary>
        /// Snapshot giới hạn Expert Evaluation.
        /// </summary>
        public int ExpertEvaluationLimit { get; private set; }

        #endregion

        #region Navigation Properties

        public virtual User Learner { get; private set; } = null!;

        public virtual SubscriptionPlan SubscriptionPlan { get; private set; } = null!;

        public virtual Payment Payment { get; private set; } = null!;

        #endregion

        private Subscription()
        {
        }

        public Subscription(
            Guid learnerId,
            Guid subscriptionPlanId,
            Guid paymentId,
            int tierLevel,
            int durationInDays,
            int aiTokenLimit,
            int aiPracticeScenarioLimit,
            int expertEvaluationLimit)
        {
            Validate(
                tierLevel,
                durationInDays,
                aiTokenLimit,
                aiPracticeScenarioLimit,
                expertEvaluationLimit);

            Id = Guid.NewGuid();

            LearnerId = learnerId;

            SubscriptionPlanId = subscriptionPlanId;

            PaymentId = paymentId;

            TierLevel = tierLevel;

            DurationInDays = durationInDays;

            AiTokenLimit = aiTokenLimit;

            AiPracticeScenarioLimit = aiPracticeScenarioLimit;

            ExpertEvaluationLimit = expertEvaluationLimit;

            ActivatedAt = DateTime.UtcNow;

            ExpiredAt = ActivatedAt.AddDays(DurationInDays);

            Status = SubscriptionStatus.Active;
        }

        /// <summary>
        /// Gia hạn cùng gói (cùng Tier).
        /// </summary>
        public void Extend()
        {
            if (Status != SubscriptionStatus.Active)
                throw new InvalidOperationException(
                    "Chỉ gói đăng ký đang hoạt động mới có thể được gia hạn.");

            ExpiredAt = ExpiredAt.AddDays(DurationInDays);

            UpdateTimestamp();
        }

        /// <summary>
        /// Được thay thế bởi một gói có Tier cao hơn.
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

        private static void Validate(
            int tierLevel,
            int durationInDays,
            int aiTokenLimit,
            int aiPracticeScenarioLimit,
            int expertEvaluationLimit)
        {
            if (tierLevel <= 0)
                throw new ArgumentException(
                    "Cấp độ gói không hợp lệ.",
                    nameof(tierLevel));

            if (durationInDays <= 0)
                throw new ArgumentException(
                    "Thời hạn gói không hợp lệ.",
                    nameof(durationInDays));

            if (aiTokenLimit < 0)
                throw new ArgumentException(
                    "Giới hạn AI Token không hợp lệ.",
                    nameof(aiTokenLimit));

            if (aiPracticeScenarioLimit < 0)
                throw new ArgumentException(
                    "Giới hạn AI Practice Scenario không hợp lệ.",
                    nameof(aiPracticeScenarioLimit));

            if (expertEvaluationLimit < 0)
                throw new ArgumentException(
                    "Giới hạn đánh giá bởi chuyên gia không hợp lệ.",
                    nameof(expertEvaluationLimit));
        }

        #endregion
    }
}