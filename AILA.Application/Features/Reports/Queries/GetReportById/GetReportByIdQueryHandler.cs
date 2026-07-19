using AILA.Application.Common.Interfaces;
using AILA.Application.Features.Reports.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Reports.Queries.GetReportById
{
    public class GetReportByIdQueryHandler(IUnitOfWork uow)
        : IRequestHandler<GetReportByIdQuery, ResponseDto<ReportDetailDto>>
    {
        public async Task<ResponseDto<ReportDetailDto>> Handle(
            GetReportByIdQuery request,
            CancellationToken ct)
        {
            var report = await uow.ContentReports.GetReportByIdAsync(
                request.ReportId,
                ct);

            if (report == null)
            {
                return ResponseDto<ReportDetailDto>.FailResult(
                    "REPORT_NOT_FOUND",
                    "Không tìm thấy báo cáo.");
            }

            var dto = new ReportDetailDto(
              report.Id,
              report.LearnerId,
              report.Learner.User.FullName,
              report.CourseId,
              report.Course?.Name,
              report.MaterialId,
              report.Material?.Title,
              report.ReportType,
              report.Description,
              report.Status,
              report.CreatedAt,
              report.ResolvedAt
          );

            return ResponseDto<ReportDetailDto>.SuccessResult(dto);
        }
    }
}