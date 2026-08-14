using AILA.Application.Features.SubscriptionPlans.Dtos;
using AILA.Domain.Entities;

namespace AILA.Application.Features.SubscriptionPlans.Mapping
{
    public static class SubscriptionPlanMapper
    {
        /// <summary>
        /// UC-09 - Map sang DTO công khai. Chủ ý bỏ Status/TierLevel/DisplayOrder
        /// để không rò rỉ dữ liệu quản trị ra endpoint công khai.
        /// <paramref name="activeTier"/> là tier gói Active của caller (null = guest hoặc chưa
        /// có gói), chỉ dùng để tính sẵn PurchaseAction — số tier không đi ra ngoài.
        /// </summary>
        public static SubscriptionPlanDto ToPublicDto(
            this SubscriptionPlan plan,
            int? activeTier = null)
            => new(
                plan.Id,
                plan.Name,
                plan.Description,
                plan.Price,
                plan.DurationInDays,
                plan.AiTokenLimit,
                plan.AiPracticeScenarioLimit,
                plan.ExpertEvaluationLimit,
                PlanPurchaseActions.Resolve(plan.TierLevel, activeTier));

        public static AdminSubscriptionPlanDto ToAdminDto(this SubscriptionPlan plan)
            => new(
                plan.Id,
                plan.Name,
                plan.Description,
                plan.Price,
                plan.TierLevel,
                plan.DurationInDays,
                plan.AiTokenLimit,
                plan.AiPracticeScenarioLimit,
                plan.ExpertEvaluationLimit,
                plan.DisplayOrder,
                plan.Status.ToString(),
                plan.CreatedAt,
                plan.UpdatedAt);
    }
}
