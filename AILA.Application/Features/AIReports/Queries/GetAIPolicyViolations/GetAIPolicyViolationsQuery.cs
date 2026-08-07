using AILA.Application.Features.AIReports.Dtos;
using MediatR;

namespace AILA.Application.Features.AIReports.Queries.GetAIPolicyViolations;

public record GetAIPolicyViolationsQuery(
    string? ViolationType = null, 
    string? Severity = null, 
    int PageNumber = 1, 
    int PageSize = 20) : IRequest<PaginatedViolationListDto>;
