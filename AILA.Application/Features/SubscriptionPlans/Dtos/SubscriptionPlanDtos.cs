namespace AILA.Application.Features.SubscriptionPlans.Dtos
{
    /// <summary>
    /// UC-09 - Dữ liệu gói hiển thị cho guest/learner trên endpoint công khai.
    /// KHÔNG chứa Status, TierLevel hay bất kỳ dữ liệu quản trị nào (TC UC-09).
    /// <para>
    /// PurchaseAction (<see cref="PlanPurchaseActions"/>) là kết quả so tier đã tính sẵn ở BE
    /// cho caller hiện tại — UI gating theo BR-05 mà không cần biết số tier.
    /// Guest hoặc learner chưa có gói Active → luôn là BUY.
    /// </para>
    /// </summary>
    public record SubscriptionPlanDto(
        Guid Id,
        string Name,
        string? Description,
        decimal Price,
        int DurationInDays,
        int AiTokenLimit,
        int AiPracticeScenarioLimit,
        int ExpertEvaluationLimit,
        string PurchaseAction);

    /// <summary>
    /// UC-90/UC-91/UC-92 - Dữ liệu gói đầy đủ cho màn quản trị.
    /// </summary>
    public record AdminSubscriptionPlanDto(
        Guid Id,
        string Name,
        string? Description,
        decimal Price,
        int TierLevel,
        int DurationInDays,
        int AiTokenLimit,
        int AiPracticeScenarioLimit,
        int ExpertEvaluationLimit,
        int DisplayOrder,
        string Status,
        DateTime CreatedAt,
        DateTime? UpdatedAt);
}
