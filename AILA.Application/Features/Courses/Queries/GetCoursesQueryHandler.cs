using AILA.Application.Common.Dtos;
using AILA.Application.Common.Interfaces;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Courses.Queries
{
    public class GetCoursesQueryHandler
        : IRequestHandler<GetCoursesQuery, PageResult<CourseListItemDto>>
    {
        private readonly IUnitOfWork _uow;

        public GetCoursesQueryHandler(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<PageResult<CourseListItemDto>> Handle(
            GetCoursesQuery request,
            CancellationToken cancellationToken)
        {
            var (courses, totalCount) = await _uow.Courses.SearchCoursesAsync(
                request.Keyword,
                request.CategoryId,
                request.TagId,
                request.Level,
                request.PageIndex,
                request.PageSize);

            var items = courses.Select(c => new CourseListItemDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                ThumbnailUrl = c.ThumbnailUrl,
                Level = c.Level.ToString(),
                DurationHours = c.DurationHours,

                Category = new CategoryDto
                {
                    Id = c.Category.Id,
                    Name = c.Category.Name,
                    Description = c.Category.Description,
                    OrderIndex = c.Category.OrderIndex
                },

                Author = new AuthorDto
                {
                    UserId = c.Expert.UserId,
                    FullName = c.Expert.User.FullName,
                    AvatarUrl = c.Expert.User.AvatarUrl,
                    Specialty = c.Expert.Specialty,
                    Bio = c.Expert.Bio,
                    YearsOfExperience = c.Expert.YearsOfExperience
                },

                Tags = c.CourseTags.Select(t => new TagDto
                {
                    Id = t.Id,
                    Name = t.Name,
                    Code = t.Code
                }).ToList()
            }).ToList();

            return new PageResult<CourseListItemDto>(
                items,
                totalCount,
                request.PageIndex,
                request.PageSize);
        }
    }
}
