using System;
using System.Collections.Generic;

namespace AILA.Application.Features.AIReports.Dtos;

public class AIServiceBreakdownResponseDto
{
    public DateTime? PeriodStart { get; set; }
    public DateTime? PeriodEnd { get; set; }
    public long TotalTokens { get; set; }
    public decimal TotalEstimatedCostUsd { get; set; }
    public decimal TotalEstimatedCostVnd { get; set; }
    public List<AIServiceBreakdownItemDto> Services { get; set; } = new();
}

public class AIServiceBreakdownItemDto
{
    public string ServiceType { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public long PromptTokens { get; set; }
    public long CompletionTokens { get; set; }
    public long TotalTokens { get; set; }
    public int RequestCount { get; set; }
    public decimal EstimatedCostUsd { get; set; }
    public decimal EstimatedCostVnd { get; set; }
    public double Percentage { get; set; }
}
