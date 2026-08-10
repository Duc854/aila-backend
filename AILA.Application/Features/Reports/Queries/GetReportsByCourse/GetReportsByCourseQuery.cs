using AILA.Application.Features.Reports.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Reports.Queries.GetReportsByCourse;

public sealed class GetReportsByCourseQuery : IRequest<ResponseDto<List<ReportDto>>>
{
    public Guid CourseId { get; }

    public GetReportsByCourseQuery(Guid courseId)
    {
        CourseId = courseId;
    }
}
