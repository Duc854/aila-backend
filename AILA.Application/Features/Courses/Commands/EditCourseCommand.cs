using AILA.Application.Common.Dtos;
using MediatR;

namespace AILA.Application.Features.Courses.Commands
{
    public record EditCourseCommand(
        Guid CourseId,
        Guid ExpertId,
        string Name,
        Guid CategoryId,
        string Level,
        string? Description,
        string? ThumbnailUrl,
        List<Guid> TagIds
    ) : IRequest<CourseManageResultDto>;
}
