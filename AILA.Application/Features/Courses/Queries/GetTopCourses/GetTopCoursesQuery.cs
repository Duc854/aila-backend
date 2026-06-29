using AILA.Application.Common.Interfaces;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Courses.Queries.GetTopCourses
{
    public class GetTopCoursesQuery : IRequest<ResponseDto<List<TopCourseResponse>>>
    {
        public int Count { get; set; } = 5;
    }

    public class TopCourseResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? ThumbnailUrl { get; set; }
        public string Level { get; set; } = string.Empty;
        public string? CategoryName { get; set; }
    }

    public class GetTopCoursesQueryHandler : IRequestHandler<GetTopCoursesQuery, ResponseDto<List<TopCourseResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetTopCoursesQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ResponseDto<List<TopCourseResponse>>> Handle(GetTopCoursesQuery request, CancellationToken cancellationToken)
        {
            var result = await _unitOfWork.Courses.SearchCoursesAsync(
                keyword: null,
                categoryId: null,
                tagId: null,
                level: null,
                pageIndex: 0,
                pageSize: request.Count);

            var courses = result.Items.Select(c => new TopCourseResponse
            {
                Id = c.Id,
                Name = c.Name,
                ThumbnailUrl = c.ThumbnailUrl,
                Level = c.Level.ToString(),
                CategoryName = c.Category?.Name
            }).ToList();

            return ResponseDto<List<TopCourseResponse>>.SuccessResult(courses);
        }
    }
}
