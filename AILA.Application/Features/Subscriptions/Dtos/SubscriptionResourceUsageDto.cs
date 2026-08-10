using System;
using System.Collections.Generic;

namespace AILA.Application.Features.Subscriptions.Dtos
{
    public class SubscriptionResourceUsageDto
    {
        public bool HasActiveSubscription { get; set; }
        public Guid? SubscriptionId { get; set; }
        public Guid? SubscriptionPlanId { get; set; }
        public string SubscriptionPlanName { get; set; } = string.Empty;
        public int? TierLevel { get; set; }
        public DateTime? ActivatedAt { get; set; }
        public DateTime? ExpiredAt { get; set; }
        public int RemainingDays { get; set; }

        public List<ResourceUsageItemDto> Resources { get; set; } = new();
    }

    public class ResourceUsageItemDto
    {
        public string ResourceType { get; set; } = string.Empty;
        public string ResourceName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public int AllocatedQuota { get; set; }
        public int UsedQuota { get; set; }
        public int RemainingQuota { get; set; }
    }
}
