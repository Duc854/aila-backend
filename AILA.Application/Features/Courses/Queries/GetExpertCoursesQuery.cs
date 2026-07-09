using AILA.Application.Common.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Courses.Queries
{
    public record GetExpertCoursesQuery(
        Guid ExpertId,
        string? Keyword = null,
        bool? IsPublished = null,
        int PageIndex = 0,
        int PageSize = 12
    ) : IRequest<PageResult<ExpertCourseListItemDto>>;
}
