using AILA.Application.Features.Reports.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Reports.Commands.DismissReport;

/// <summary>
/// Admin từ chối (bác bỏ) báo cáo — nội dung không vi phạm.
/// Domain không có status Rejected, nên dùng Resolved với message phân biệt.
/// </summary>
public sealed record DismissReportCommand(
    Guid ReportId,
    string? Note
) : IRequest<ResponseDto<ResolveReportResponseDto>>;
