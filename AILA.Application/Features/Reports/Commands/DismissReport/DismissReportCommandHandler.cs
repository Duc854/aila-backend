using AILA.Application.Common.Interfaces;
using AILA.Application.Features.Reports.Dtos;
using AILA.Domain.Enums;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Reports.Commands.DismissReport;

public sealed class DismissReportCommandHandler
    : IRequestHandler<DismissReportCommand, ResponseDto<ResolveReportResponseDto>>
{
    private readonly IUnitOfWork _uow;

    public DismissReportCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<ResponseDto<ResolveReportResponseDto>> Handle(
        DismissReportCommand request,
        CancellationToken ct)
    {
        if (request.ReportId == Guid.Empty)
            return ResponseDto<ResolveReportResponseDto>.FailResult(
                "INVALID_REPORT_ID", "Report ID không hợp lệ.");

        var report = await _uow.ContentReports.GetByIdAsync(request.ReportId);
        if (report is null)
            return ResponseDto<ResolveReportResponseDto>.FailResult(
                "REPORT_NOT_FOUND", "Không tìm thấy báo cáo.");

        if (report.Status != ReportStatus.Pending)
            return ResponseDto<ResolveReportResponseDto>.FailResult(
                "ALREADY_RESOLVED", "Báo cáo đã được xử lý.");

        // Domain chỉ có Resolve — dùng chung, phân biệt qua message
        report.Resolve();
        await _uow.SaveChangesAsync(ct);

        return ResponseDto<ResolveReportResponseDto>.SuccessResult(new ResolveReportResponseDto
        {
            ReportId   = report.Id,
            Status     = report.Status.ToString(),
            ResolvedAt = report.ResolvedAt!.Value,
            Message    = "Đã từ chối báo cáo — nội dung không vi phạm."
        });
    }
}
