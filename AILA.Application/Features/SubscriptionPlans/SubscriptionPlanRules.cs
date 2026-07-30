namespace AILA.Application.Features.SubscriptionPlans
{
    /// <summary>
    /// Kiểm tra đầu vào ở tầng application TRƯỚC khi gọi entity, để trả về mã lỗi bám đúng
    /// field vi phạm (NFR UC-90/UC-91) thay vì để <see cref="ArgumentException"/> của domain
    /// bật lên thành lỗi 500. Domain vẫn là hàng rào cuối cùng — mọi rule ở đây phải khớp
    /// 1-1 với validation trong <c>SubscriptionPlan</c>.
    /// </summary>
    public static class SubscriptionPlanRules
    {
        /// <summary>Khớp HasMaxLength(100) của SubscriptionPlanConfiguration.</summary>
        public const int NameMaxLength = 100;

        /// <summary>Khớp HasMaxLength(1000) của SubscriptionPlanConfiguration.</summary>
        public const int DescriptionMaxLength = 1000;

        /// <summary>
        /// Rule dùng chung cho cả tạo và cập nhật. Trả về null nếu hợp lệ.
        /// </summary>
        public static (string Code, string Message)? ValidateCommonFields(
            string? description,
            decimal price,
            int aiTokenLimit,
            int aiPracticeScenarioLimit,
            int expertEvaluationLimit,
            int displayOrder)
        {
            if (description is not null && description.Trim().Length > DescriptionMaxLength)
                return (SubscriptionPlanErrors.DescriptionTooLong,
                    $"Mô tả gói không được vượt quá {DescriptionMaxLength} ký tự.");

            if (price <= 0)
                return (SubscriptionPlanErrors.InvalidPrice,
                    "Giá gói phải lớn hơn 0.");

            if (aiTokenLimit < 0)
                return (SubscriptionPlanErrors.InvalidAiTokenLimit,
                    "Giới hạn AI Token không hợp lệ.");

            if (aiPracticeScenarioLimit < 0)
                return (SubscriptionPlanErrors.InvalidAiPracticeScenarioLimit,
                    "Giới hạn số lần AI Practice không hợp lệ.");

            if (expertEvaluationLimit < 0)
                return (SubscriptionPlanErrors.InvalidExpertEvaluationLimit,
                    "Giới hạn số lần đánh giá chuyên gia không hợp lệ.");

            if (displayOrder < 0)
                return (SubscriptionPlanErrors.InvalidDisplayOrder,
                    "Thứ tự hiển thị không hợp lệ.");

            return null;
        }

        /// <summary>
        /// Rule chỉ áp khi tạo: Name, TierLevel, DurationInDays đều bất biến hoặc
        /// không sửa được qua UC-91 nên chỉ kiểm ở đây.
        /// </summary>
        public static (string Code, string Message)? ValidateCreateOnlyFields(
            string? name,
            int tierLevel,
            int durationInDays)
        {
            if (string.IsNullOrWhiteSpace(name))
                return (SubscriptionPlanErrors.NameRequired,
                    "Tên gói đăng ký không được để trống.");

            if (name.Trim().Length > NameMaxLength)
                return (SubscriptionPlanErrors.NameTooLong,
                    $"Tên gói đăng ký không được vượt quá {NameMaxLength} ký tự.");

            if (tierLevel <= 0)
                return (SubscriptionPlanErrors.InvalidTierLevel,
                    "Cấp độ gói phải lớn hơn 0.");

            if (durationInDays <= 0)
                return (SubscriptionPlanErrors.InvalidDuration,
                    "Thời hạn gói phải lớn hơn 0 ngày.");

            return null;
        }

        /// <summary>
        /// Map vi phạm unique index ở DB (hàng rào cuối chống race condition) sang mã lỗi
        /// validation, để không lộ lỗi hạ tầng ra ngoài (TC UC-90).
        /// </summary>
        public static (string Code, string Message) MapDuplicateConstraint(string constraintName)
        {
            if (constraintName.Contains("TierLevel", StringComparison.OrdinalIgnoreCase))
                return (SubscriptionPlanErrors.TierLevelAlreadyExists,
                    "Cấp độ gói đã tồn tại.");

            if (constraintName.Contains("Name", StringComparison.OrdinalIgnoreCase))
                return (SubscriptionPlanErrors.NameAlreadyExists,
                    "Tên gói đăng ký đã tồn tại.");

            return (SubscriptionPlanErrors.ValidationError,
                "Gói đăng ký bị trùng với một gói đã tồn tại.");
        }
    }
}
