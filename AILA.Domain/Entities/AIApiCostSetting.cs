using AILA.Domain.Common;
using System;

namespace AILA.Domain.Entities
{
    public class AIApiCostSetting : BaseEntity
    {
        public string ModelId { get; private set; } = string.Empty;
        public string ServiceName { get; private set; } = string.Empty;
        public decimal CostPerInputToken { get; private set; }
        public decimal CostPerOutputToken { get; private set; }
        public string Currency { get; private set; } = "USD";
        public bool IsActive { get; private set; } = true;

        // EF Core constructor
        private AIApiCostSetting() { }

        public AIApiCostSetting(
            string modelId,
            string serviceName,
            decimal costPerInputToken,
            decimal costPerOutputToken,
            string currency = "USD",
            bool isActive = true)
        {
            if (string.IsNullOrWhiteSpace(modelId))
                throw new ArgumentException("ModelId không được để trống.", nameof(modelId));

            Id = Guid.NewGuid();
            ModelId = modelId;
            ServiceName = serviceName ?? "Groq";
            CostPerInputToken = costPerInputToken;
            CostPerOutputToken = costPerOutputToken;
            Currency = currency ?? "USD";
            IsActive = isActive;
        }

        public void UpdatePricing(decimal costPerInputToken, decimal costPerOutputToken, bool isActive)
        {
            CostPerInputToken = costPerInputToken;
            CostPerOutputToken = costPerOutputToken;
            IsActive = isActive;
            UpdateTimestamp();
        }
    }
}
