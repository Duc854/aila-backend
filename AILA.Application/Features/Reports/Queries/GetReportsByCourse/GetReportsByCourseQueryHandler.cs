using AILA.Application.Common.Interfaces;
using AILA.Application.Features.Reports.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Reports.Queries.GetReportsByCourse;

public sealed class GetReportsByCourseQueryHandler
    : IRequestHandler<GetReportsByCourseQuery, ResponseDto<List<ReportDto>>>
{
    private readonly IUnitOfWork _uow;

    public GetReportsByCourseQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<ResponseDto<List<ReportDto>>> Handle(
        GetReportsByCourseQuery request,
        CancellationToken ct)
    {
        var reports = await _uow.ContentReports
            .GetReportsByCourseAsync(request.CourseId, ct);

        var result = reports.Select(r => new ReportDto
        {
            Id           = r.Id,
            CourseId     = r.CourseId,
            MaterialId   = r.MaterialId,
            CourseName   = r.Course?.Name ?? r.Material?.Module?.Course?.Name,
            MaterialName = r.Material?.Title,
            ContentType  = r.MaterialId.HasValue ? "Learning Material" : "Course",
            IsCourseLocked = r.Course?.IsPublicationLocked
                          ?? r.Material?.Module?.Course?.IsPublicationLocked,
            LearnerName  = r.Learner?.User?.FullName,
            Reason       = r.ReportType.ToString(),
            Description  = r.Description,
            Status       = r.Status.ToString(),
            CreatedAt    = r.CreatedAt,
            ResolvedAt   = r.ResolvedAt,
        }).ToList();

        return ResponseDto<List<ReportDto>>.SuccessResult(result);
    }
}
