namespace AILA.Application.Features.Subscriptions.Dtos
{
    /// <summary>
    /// UC-18 BR-02: Thông tin gói đăng ký hiện tại của learner.
    /// </summary>
    public record CurrentSubscriptionDto(
        bool HasActiveSubscription,
        Guid? SubscriptionId,
        string SubscriptionPlanName,
        int? TierLevel,
        string Status,
        DateTime? ActivatedAt,
        DateTime? ExpiredAt,
        int RemainingDays);
}
