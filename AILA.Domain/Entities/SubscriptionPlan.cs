using AILA.Domain.Common;
using AILA.Domain.Enums;

namespace AILA.Domain.Entities
{
    public class SubscriptionPlan : BaseEntity
    {
        public string Name { get; private set; }

        public string? Description { get; private set; }

        public decimal Price { get; private set; }

        public int TierLevel { get; private set; }

        public int DurationInDays { get; private set; }

        public int AiTokenLimit { get; private set; }

        public int AiPracticeScenarioLimit { get; private set; }

        public int ExpertEvaluationLimit { get; private set; }

        public int DisplayOrder { get; private set; }

        public SubscriptionPlanStatus Status { get; private set; }

        #region Navigation Properties

        private readonly List<Subscription> _subscriptions = new();

        public virtual IReadOnlyCollection<Subscription> Subscriptions
            => _subscriptions.AsReadOnly();


        private readonly List<Payment> _payments = new();

        public IReadOnlyCollection<Payment> Payments => _payments.AsReadOnly();

        #endregion

        private SubscriptionPlan()
        {
        }

        public SubscriptionPlan(
            string name,
            string? description,
            decimal price,
            int tierLevel,
            int durationInDays,
            int aiTokenLimit,
            int aiPracticeScenarioLimit,
            int expertEvaluationLimit,
            int displayOrder)
        {
            Validate(
                name,
                price,
                tierLevel,
                durationInDays,
                aiTokenLimit,
                aiPracticeScenarioLimit,
                expertEvaluationLimit,
                displayOrder);

            Id = Guid.NewGuid();

            Name = name.Trim();

            Description = description?.Trim();

            Price = price;

            TierLevel = tierLevel;

            DurationInDays = durationInDays;

            AiTokenLimit = aiTokenLimit;

            AiPracticeScenarioLimit = aiPracticeScenarioLimit;

            ExpertEvaluationLimit = expertEvaluationLimit;

            DisplayOrder = displayOrder;

            Status = SubscriptionPlanStatus.Active;
        }

        public void Update(
            string? description,
            decimal price,
            int aiTokenLimit,
            int aiPracticeScenarioLimit,
            int expertEvaluationLimit,
            int displayOrder)
        {
            ValidatePrice(price);

            ValidateQuota(
                aiTokenLimit,
                aiPracticeScenarioLimit,
                expertEvaluationLimit);

            if (displayOrder < 0)
                throw new ArgumentException(
                    "Thứ tự hiển thị không hợp lệ.",
                    nameof(displayOrder));

            Description = description?.Trim();

            Price = price;

            AiTokenLimit = aiTokenLimit;

            AiPracticeScenarioLimit = aiPracticeScenarioLimit;

            ExpertEvaluationLimit = expertEvaluationLimit;

            DisplayOrder = displayOrder;

            UpdateTimestamp();
        }

        public void Activate()
        {
            if (Status == SubscriptionPlanStatus.Active)
                throw new ArgumentException(
                    "Gói đăng ký đã ở trạng thái hoạt động.");

            Status = SubscriptionPlanStatus.Active;

            UpdateTimestamp();
        }

        public void Deactivate()
        {
            if (Status == SubscriptionPlanStatus.Inactive)
                throw new ArgumentException(
                    "Gói đăng ký đã ở trạng thái ngừng hoạt động.");

            Status = SubscriptionPlanStatus.Inactive;

            UpdateTimestamp();
        }

        public bool IsActive()
        {
            return Status == SubscriptionPlanStatus.Active;
        }

        #region Validation

        private static void Validate(
            string name,
            decimal price,
            int tierLevel,
            int durationInDays,
            int aiTokenLimit,
            int aiPracticeScenarioLimit,
            int expertEvaluationLimit,
            int displayOrder)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException(
                    "Tên gói đăng ký không được để trống.",
                    nameof(name));

            ValidatePrice(price);

            if (tierLevel <= 0)
                throw new ArgumentException(
                    "Cấp độ gói phải lớn hơn 0.",
                    nameof(tierLevel));

            if (durationInDays <= 0)
                throw new ArgumentException(
                    "Thời hạn gói phải lớn hơn 0 ngày.",
                    nameof(durationInDays));

            ValidateQuota(
                aiTokenLimit,
                aiPracticeScenarioLimit,
                expertEvaluationLimit);

            if (displayOrder < 0)
                throw new ArgumentException(
                    "Thứ tự hiển thị không hợp lệ.",
                    nameof(displayOrder));
        }

        private static void ValidatePrice(decimal price)
        {
            if (price <= 0)
                throw new ArgumentException(
                    "Giá gói phải lớn hơn 0.",
                    nameof(price));
        }

        private static void ValidateQuota(
            int aiTokenLimit,
            int aiPracticeScenarioLimit,
            int expertEvaluationLimit)
        {
            if (aiTokenLimit < 0)
                throw new ArgumentException(
                    "Giới hạn AI Token không hợp lệ.",
                    nameof(aiTokenLimit));

            if (aiPracticeScenarioLimit < 0)
                throw new ArgumentException(
                    "Giới hạn số lần AI Practice không hợp lệ.",
                    nameof(aiPracticeScenarioLimit));

            if (expertEvaluationLimit < 0)
                throw new ArgumentException(
                    "Giới hạn số lần đánh giá chuyên gia không hợp lệ.",
                    nameof(expertEvaluationLimit));
        }

        #endregion
    }
}