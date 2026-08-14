namespace AILA.Application.Features.SubscriptionPlans
{
    /// <summary>
    /// UC-09/UC-19 — Hành động mua mà caller được phép thực hiện trên một gói, tính sẵn ở BE
    /// bằng cách so tier của gói với tier của gói Active hiện tại.
    /// Trả action thay vì TierLevel để UI gating đúng BR-05 mà endpoint công khai vẫn không
    /// rò rỉ dữ liệu quản trị (TC UC-09).
    /// </summary>
    public static class PlanPurchaseActions
    {
        /// <summary>Chưa có gói Active (hoặc caller là guest) → mua mới.</summary>
        public const string Buy = "BUY";

        /// <summary>Trùng tier với gói đang dùng → gia hạn.</summary>
        public const string Renew = "RENEW";

        /// <summary>Tier cao hơn gói đang dùng → nâng cấp.</summary>
        public const string Upgrade = "UPGRADE";

        /// <summary>BR-05: tier thấp hơn gói đang dùng → không cho mua.</summary>
        public const string Blocked = "BLOCKED";

        /// <summary>
        /// So tier gói với tier đang Active. <paramref name="activeTier"/> null = chưa có gói.
        /// </summary>
        public static string Resolve(int planTier, int? activeTier)
        {
            if (activeTier is null) return Buy;
            if (planTier > activeTier) return Upgrade;
            if (planTier == activeTier) return Renew;
            return Blocked;
        }
    }
}
