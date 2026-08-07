using System;
using System.Collections.Generic;

namespace AILA.Application.Features.AIReports.Dtos;

public class AIPolicyViolationDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? AttemptId { get; set; }
    public string ViolationType { get; set; } = string.Empty;
    public string PolicyName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class PaginatedViolationListDto
{
    public List<AIPolicyViolationDto> Items { get; set; } = new();
    public int PageNumber { get; set; }
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}
