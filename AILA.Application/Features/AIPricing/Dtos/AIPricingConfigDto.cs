using System;

namespace AILA.Application.Features.AIPricing.Dtos;

public class AIPricingConfigDto
{
    public Guid Id { get; set; }
    public string ModelId { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public decimal CostPerInputToken { get; set; }
    public decimal CostPerOutputToken { get; set; }
    public string Currency { get; set; } = "USD";
    public bool IsActive { get; set; }
}

public class UpdateAIPricingRequest
{
    public string ModelId { get; set; } = string.Empty;
    public string ServiceName { get; set; } = "Groq";
    public decimal CostPerInputToken { get; set; }
    public decimal CostPerOutputToken { get; set; }
    public string Currency { get; set; } = "USD";
    public bool IsActive { get; set; } = true;
}
