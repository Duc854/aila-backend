using MediatR;
using Shared.Wrappers;
using AILA.Application.Features.Reports.Dtos;

namespace AILA.Application.Features.Reports.Commands.ResolveReport
{
    public class ResolveReportCommand : IRequest<ResponseDto<ResolveReportResponseDto>>
    {
        public Guid ReportId { get; set; }

        // ✅ Thêm constructor
        public ResolveReportCommand(Guid reportId)
        {
            ReportId = reportId;
        }

        // ✅ Thêm constructor mặc định (cho MediatR)
        public ResolveReportCommand() { }
    }
}