using AILA.Domain.Enums;
using MediatR;
using Shared.Wrappers;
using System.Collections.Generic;
using AILA.Application.Features.Reports.Dtos;

namespace AILA.Application.Features.Reports.Queries.GetReports
{
    public class GetReportsQuery : IRequest<ResponseDto<List<ReportDto>>>
    {
        public ReportStatus? Status { get; set; }
        public bool? IsCourseReport { get; set; }

        // ✅ Thêm constructor
        public GetReportsQuery(ReportStatus? status, bool? isCourseReport)
        {
            Status = status;
            IsCourseReport = isCourseReport;
        }

        // ✅ Thêm constructor mặc định (cho MediatR)
        public GetReportsQuery() { }
    }
}
