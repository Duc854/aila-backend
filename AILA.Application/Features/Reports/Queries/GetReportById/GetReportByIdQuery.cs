using AILA.Application.Features.Reports.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Reports.Queries.GetReportById
{
    public record GetReportByIdQuery(
        Guid ReportId
    ) : IRequest<ResponseDto<ReportDetailDto>>;
}