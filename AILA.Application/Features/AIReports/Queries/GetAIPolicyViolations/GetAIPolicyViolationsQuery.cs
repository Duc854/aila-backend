using AILA.Application.Features.AIReports.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.AIReports.Queries.GetAIPolicyViolations;

public record GetAIPolicyViolationsQuery(
    string? ViolationType = null,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<ResponseDto<PaginatedViolationListDto>>;
