namespace AILA.Application.Features.Subscriptions.Dtos
{
    public class SubscriptionStatisticsDto
    {
        public decimal TotalRevenue { get; set; }

        public int TotalTransactions { get; set; }

        public int TotalUniqueBuyers { get; set; }

        public int ActiveSubscriptionsCount { get; set; }

        public int ExpiredSubscriptionsCount { get; set; }

        public List<SubscriptionPlanStatDto> PlanBreakdowns { get; set; } = new();

        public List<SubscriptionRevenueTrendDto> RevenueTrends { get; set; } = new();
    }

    public class SubscriptionPlanStatDto
    {
        public Guid PlanId { get; set; }

        public string PlanName { get; set; } = string.Empty;

        public int TierLevel { get; set; }

        public decimal CurrentPrice { get; set; }

        public int TotalPurchases { get; set; }

        public decimal TotalRevenue { get; set; }

        public int ActiveCount { get; set; }
    }

    public class SubscriptionRevenueTrendDto
    {
        public string Date { get; set; } = string.Empty;

        public decimal Revenue { get; set; }

        public int TransactionCount { get; set; }
    }
}
