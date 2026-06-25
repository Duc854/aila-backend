using AILA.Application.Common.Interfaces;
using AILA.Application.Features.Courses.DTOs;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AILA.Application.Features.Courses.Queries
{
    public class GetTopCoursesQueryHandler : IRequestHandler<GetTopCoursesQuery, IReadOnlyList<CourseDto>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetTopCoursesQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IReadOnlyList<CourseDto>> Handle(GetTopCoursesQuery request, CancellationToken cancellationToken)
        {
            var courses = await _unitOfWork.Courses.GetTopCoursesAsync(request.Count);

            return courses.Select(c => new CourseDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                ThumbnailUrl = c.ThumbnailUrl,
                Level = c.Level.ToString(),
                DurationHours = c.DurationHours
            }).ToList().AsReadOnly();
        }
    }
}
