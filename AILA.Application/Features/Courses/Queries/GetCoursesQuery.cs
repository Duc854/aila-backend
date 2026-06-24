using AILA.Application.Common.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Courses.Queries
{
    public record GetCoursesQuery(
        string? Keyword,
        Guid? CategoryId,
        Guid? TagId,
        string? Level,
        int PageIndex = 0,
        int PageSize = 12
    ) : IRequest<PageResult<CourseListItemDto>>;
}
