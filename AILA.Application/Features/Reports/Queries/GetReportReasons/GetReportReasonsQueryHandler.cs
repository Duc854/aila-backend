using AILA.Application.Features.Reports.Dtos;
using AILA.Domain.Enums;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Reports.Queries.GetReportReasons
{
    public class GetReportReasonsQueryHandler
        : IRequestHandler<GetReportReasonsQuery, ResponseDto<IEnumerable<ReportReasonDto>>>
    {
        public Task<ResponseDto<IEnumerable<ReportReasonDto>>> Handle(
            GetReportReasonsQuery request, CancellationToken ct)
        {
            var reasons = Enum.GetValues<ReportType>()
                .Select(r => new ReportReasonDto((int)r, r.ToString()))
                .ToList();

            return Task.FromResult(
                ResponseDto<IEnumerable<ReportReasonDto>>.SuccessResult(reasons));
        }
    }
}
