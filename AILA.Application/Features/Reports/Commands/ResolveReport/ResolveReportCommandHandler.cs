using System;
using System.Threading;
using System.Threading.Tasks;
using AILA.Application.Common.Interfaces;
using AILA.Application.Features.Reports.Dtos;
using AILA.Domain.Enums;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Reports.Commands.ResolveReport
{
    public class ResolveReportCommandHandler : IRequestHandler<ResolveReportCommand, ResponseDto<ResolveReportResponseDto>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public ResolveReportCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ResponseDto<ResolveReportResponseDto>> Handle(
            ResolveReportCommand request,
            CancellationToken cancellationToken)
        {
            // ✅ Validate Report ID
            if (request.ReportId == Guid.Empty)
            {
                return ResponseDto<ResolveReportResponseDto>.FailResult(
                    "INVALID_REPORT_ID",
                    "Report ID không hợp lệ.");
            }

            // ✅ Get report
            var report = await _unitOfWork.ContentReports.GetByIdAsync(request.ReportId);
            if (report == null)
            {
                return ResponseDto<ResolveReportResponseDto>.FailResult(
                    "REPORT_NOT_FOUND",
                    "Không tìm thấy báo cáo.");
            }

            // ✅ BR-04: Only Pending can be Resolved
            if (report.Status != ReportStatus.Pending)
            {
                return ResponseDto<ResolveReportResponseDto>.FailResult(
                    "ALREADY_RESOLVED",
                    "Báo cáo đã được xử lý."); // AF-02
            }

            // ✅ Mark as Resolved (Domain method)
            report.MarkAsResolved();

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return ResponseDto<ResolveReportResponseDto>.SuccessResult(new ResolveReportResponseDto
            {
                ReportId = report.Id,
                Status = report.Status.ToString(),
                ResolvedAt = report.ResolvedAt.Value,
                Message = "Đã đánh dấu báo cáo là đã giải quyết."
            });
        }
    }
}