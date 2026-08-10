using System;
using System.Collections.Generic;

namespace AILA.Application.Features.AIReports.Dtos;

public class AIResourceConsumptionReportDto
{
    public long TotalPromptTokens { get; set; }
    public long TotalCompletionTokens { get; set; }
    public long TotalTokens { get; set; }
    public int TotalRequests { get; set; }
    public decimal TotalEstimatedCostUsd { get; set; }
    public DateTime? PeriodStart { get; set; }
    public DateTime? PeriodEnd { get; set; }
    public List<AIModelUsageDto> ModelBreakdown { get; set; } = new();
}

public class AIModelUsageDto
{
    public string ModelId { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public long PromptTokens { get; set; }
    public long CompletionTokens { get; set; }
    public long TotalTokens { get; set; }
    public int RequestCount { get; set; }
    public decimal EstimatedCostUsd { get; set; }
}
