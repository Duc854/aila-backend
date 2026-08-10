using System;

namespace AILA.Application.Features.Experts.Dtos
{
    public class ExpertAiResourceUsageDto
    {
        public Guid AccountId { get; set; }
        public int AllocatedTokens { get; set; }
        public int ConsumedTokens { get; set; }
        public int RemainingTokens { get; set; }
        public decimal UsagePercentage { get; set; }
        public bool IsNearLimit { get; set; }
        public bool IsExceeded { get; set; }
        public string StatusMessage { get; set; } = string.Empty;
    }
}
