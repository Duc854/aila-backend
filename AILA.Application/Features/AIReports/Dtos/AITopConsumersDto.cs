using System;
using System.Collections.Generic;

namespace AILA.Application.Features.AIReports.Dtos;

public class AITopConsumersResponseDto
{
    public DateTime? PeriodStart { get; set; }
    public DateTime? PeriodEnd { get; set; }
    public List<TopUserConsumerDto> TopUsers { get; set; } = new();
    public List<TopMaterialConsumerDto> TopMaterials { get; set; } = new();
}

public class TopUserConsumerDto
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public long TotalTokens { get; set; }
    public int RequestCount { get; set; }
    public decimal EstimatedCostUsd { get; set; }
    public decimal EstimatedCostVnd { get; set; }
}

public class TopMaterialConsumerDto
{
    public Guid MaterialId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string CourseTitle { get; set; } = string.Empty;
    public int TotalAttempts { get; set; }
    public long TotalTokens { get; set; }
    public decimal EstimatedCostUsd { get; set; }
    public decimal EstimatedCostVnd { get; set; }
}
