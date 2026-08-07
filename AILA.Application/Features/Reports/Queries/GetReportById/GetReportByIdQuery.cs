using MediatR;
using Shared.Wrappers;
using AILA.Application.Features.Reports.Dtos;

namespace AILA.Application.Features.Reports.Queries.GetReportById
{
    public class GetReportByIdQuery : IRequest<ResponseDto<ReportDetailDto>>
    {
        public Guid ReportId { get; set; }

        // ✅ Thêm constructor
        public GetReportByIdQuery(Guid reportId)
        {
            ReportId = reportId;
        }

        // ✅ Thêm constructor mặc định (cho MediatR)
        public GetReportByIdQuery() { }
    }
}
