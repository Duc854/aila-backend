using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Courses.Commands
{
    public record UnpublishCourseCommand(Guid CourseId, Guid ExpertId) : IRequest<ResponseDto<object>>;
}
