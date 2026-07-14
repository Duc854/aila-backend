using AILA.Application.Common.Interfaces;
using AILA.Application.Features.Reports.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Reports.Queries.GetReports
{
    public class GetReportsQueryHandler(IUnitOfWork uow)
        : IRequestHandler<GetReportsQuery, ResponseDto<IEnumerable<ReportListItemDto>>>
    {
        public async Task<ResponseDto<IEnumerable<ReportListItemDto>>> Handle(
            GetReportsQuery request,
            CancellationToken ct)
        {
            var reports = await uow.ContentReports.GetReportsAsync(
                request.Status,
                request.IsCourseReport,
                ct);

            var result = reports.Select(r => new ReportListItemDto(
                r.Id,
                r.LearnerId,
                r.CourseId,
                r.MaterialId,
                r.ReportType,
                r.Description,
                r.Status,
                r.CreatedAt
            ));

            return ResponseDto<IEnumerable<ReportListItemDto>>
                .SuccessResult(result);
        }
    }
}