using AILA.Application.Features.Reports.Dtos;
using AILA.Domain.Enums;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Reports.Queries.GetReports
{
    public record GetReportsQuery(
        ReportStatus? Status,
        bool? IsCourseReport
    ) : IRequest<ResponseDto<IEnumerable<ReportListItemDto>>>;
}