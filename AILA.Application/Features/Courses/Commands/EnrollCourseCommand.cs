using AILA.Application.Common.Dtos;
using MediatR;

namespace AILA.Application.Features.Courses.Commands
{
    public record EnrollCourseCommand(Guid CourseId, Guid LearnerId) : IRequest<EnrollmentResultDto>;
}
