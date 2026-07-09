using AILA.Application.Features.Reports.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Reports.Queries.GetReportReasons
{
    /// <summary>UC-33, AC-1 — danh sách lý do báo cáo hợp lệ để hiển thị trên form.</summary>
    public record GetReportReasonsQuery : IRequest<ResponseDto<IEnumerable<ReportReasonDto>>>;
}
