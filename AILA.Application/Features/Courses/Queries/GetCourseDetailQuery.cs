using AILA.Application.Common.Dtos;
using MediatR;

namespace AILA.Application.Features.Courses.Queries
{
    public record GetCourseDetailQuery(Guid CourseId) : IRequest<CourseDetailDto?>;
}
