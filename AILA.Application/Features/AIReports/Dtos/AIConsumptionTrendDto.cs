using System;
using System.Collections.Generic;

namespace AILA.Application.Features.AIReports.Dtos;

public class AIConsumptionTrendResponseDto
{
    public string Interval { get; set; } = "day"; // "day", "week", "month"
    public DateTime? PeriodStart { get; set; }
    public DateTime? PeriodEnd { get; set; }
    public long TotalTokens { get; set; }
    public decimal TotalEstimatedCostUsd { get; set; }
    public decimal TotalEstimatedCostVnd { get; set; }
    public List<AIConsumptionTrendPointDto> DataPoints { get; set; } = new();
}

public class AIConsumptionTrendPointDto
{
    public string Date { get; set; } = string.Empty; // "yyyy-MM-dd" or "yyyy-MM"
    public long PromptTokens { get; set; }
    public long CompletionTokens { get; set; }
    public long TotalTokens { get; set; }
    public int TotalRequests { get; set; }
    public decimal EstimatedCostUsd { get; set; }
    public decimal EstimatedCostVnd { get; set; }
}
