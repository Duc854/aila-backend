using AILA.Application.Features.Courses.DTOs;
using MediatR;
using System.Collections.Generic;

namespace AILA.Application.Features.Courses.Queries
{
    public class GetTopCoursesQuery : IRequest<IReadOnlyList<CourseDto>>
    {
        public int Count { get; set; } = 5;
    }
}
