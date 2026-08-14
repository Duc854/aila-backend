namespace AILA.Application.Features.SubscriptionPlans.Dtos
{
    /// <summary>
    /// UC-90 - Request tạo gói. Không có Status: trạng thái luôn do entity gán Active (BR-05).
    /// </summary>
    public class CreateSubscriptionPlanRequest
    {
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public decimal Price { get; set; }

        public int TierLevel { get; set; }

        public int DurationInDays { get; set; }

        public int AiTokenLimit { get; set; }

        public int AiPracticeScenarioLimit { get; set; }

        public int ExpertEvaluationLimit { get; set; }

        public int DisplayOrder { get; set; }
    }

    /// <summary>
    /// UC-91 - Request cập nhật gói. Cố ý KHÔNG có Name/TierLevel (INV-01, BR-01) —
    /// client gửi kèm cũng bị bỏ qua. DurationInDays sửa được: giá trị mới chỉ dùng cho
    /// các lượt mua/gia hạn sau, subscription đã bán giữ nguyên snapshot (INV-03, BR-04).
    /// </summary>
    public class UpdateSubscriptionPlanRequest
    {
        public string? Description { get; set; }

        public decimal Price { get; set; }

        public int DurationInDays { get; set; }

        public int AiTokenLimit { get; set; }

        public int AiPracticeScenarioLimit { get; set; }

        public int ExpertEvaluationLimit { get; set; }

        public int DisplayOrder { get; set; }
    }

    /// <summary>
    /// UC-92 - Request đổi trạng thái gói. IsActive = true → Activate, false → Deactivate.
    /// </summary>
    public class ChangeSubscriptionPlanStatusRequest
    {
        public bool IsActive { get; set; }
    }
}
