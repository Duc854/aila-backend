using System;
using System.Threading;
using System.Threading.Tasks;
using AILA.Application.Common.Interfaces;
using AILA.Application.Features.Reports.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Reports.Queries.GetReportById
{
    public class GetReportByIdQueryHandler : IRequestHandler<GetReportByIdQuery, ResponseDto<ReportDetailDto>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetReportByIdQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ResponseDto<ReportDetailDto>> Handle(
            GetReportByIdQuery request,
            CancellationToken cancellationToken)
        {
            // ✅ Validate Report ID
            if (request.ReportId == Guid.Empty)
            {
                return ResponseDto<ReportDetailDto>.FailResult(
                    "INVALID_REPORT_ID",
                    "Report ID không hợp lệ.");
            }

            // ✅ Get report with details
            var report = await _unitOfWork.ContentReports.GetReportWithDetailsAsync(
                request.ReportId,
                cancellationToken);

            if (report == null)
            {
                return ResponseDto<ReportDetailDto>.FailResult(
                    "REPORT_NOT_FOUND",
                    "Không tìm thấy báo cáo.");
            }

            // ✅ BR-03: Report references exactly one content item
            return ResponseDto<ReportDetailDto>.SuccessResult(new ReportDetailDto
            {
                Id = report.Id,
                CourseName = report.Course?.Name,
                MaterialName = report.Material?.Title,
                ContentType = report.MaterialId.HasValue ? "Learning Material" : "Course",
                LearnerName = report.Learner?.User.FullName,
                LearnerEmail = report.Learner?.User?.Email,
                Reason = report.ReportType.ToString(),
                Description = report.Description,
                Status = report.Status.ToString(),
                CreatedAt = report.CreatedAt,
                ResolvedAt = report.ResolvedAt
            });
        }
    }
}