using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AILA.Application.Common.Interfaces;
using AILA.Application.Features.Reports.Dtos;
using AILA.Domain.Enums;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Reports.Queries.GetReports
{
    public class GetReportsQueryHandler : IRequestHandler<GetReportsQuery, ResponseDto<List<ReportDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetReportsQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ResponseDto<List<ReportDto>>> Handle(
            GetReportsQuery request,
            CancellationToken cancellationToken)
        {
            // ✅ Get all reports with filter (BR-01, BR-02)
            var reports = await _unitOfWork.ContentReports.GetReportsAsync(
                  request.Status,
                  request.IsCourseReport,
                  cancellationToken);

            // ✅ AF-01: No matching reports
            if (reports == null || !reports.Any())
            {
                return ResponseDto<List<ReportDto>>.SuccessResult(
                    new List<ReportDto>());
            }

            var result = reports.Select(r => new ReportDto
            {
                Id          = r.Id,
                CourseId    = r.CourseId,
                MaterialId  = r.MaterialId,
                CourseName  = r.Course?.Name ?? r.Material?.Module?.Course?.Name,
                MaterialName = r.Material?.Title,
                ContentType  = r.MaterialId.HasValue ? "Learning Material" : "Course",
                IsCourseLocked = r.Course?.IsPublicationLocked ?? r.Material?.Module?.Course?.IsPublicationLocked,
                LearnerName  = r.Learner?.User?.FullName,
                Reason       = r.ReportType.ToString(),
                Description  = r.Description,
                Status       = r.Status.ToString(),
                CreatedAt    = r.CreatedAt,
                ResolvedAt   = r.ResolvedAt
            }).ToList();

            return ResponseDto<List<ReportDto>>.SuccessResult(result);
        }
    }
}
