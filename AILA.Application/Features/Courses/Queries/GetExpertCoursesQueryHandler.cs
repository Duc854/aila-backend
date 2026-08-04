using AILA.Application.Common.Dtos;
using AILA.Application.Common.Interfaces;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Courses.Queries
{
    public class GetExpertCoursesQueryHandler
        : IRequestHandler<GetExpertCoursesQuery, PageResult<ExpertCourseListItemDto>>
    {
        private readonly IUnitOfWork _uow;

        public GetExpertCoursesQueryHandler(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<PageResult<ExpertCourseListItemDto>> Handle(
            GetExpertCoursesQuery request,
            CancellationToken cancellationToken)
        {
            var (courses, totalCount) = await _uow.Courses.GetByExpertAsync(
                request.ExpertId,
                request.Keyword,
                request.IsPublished,
                request.PageIndex,
                request.PageSize,
                cancellationToken);

            var items = courses.Select(c => new ExpertCourseListItemDto
            {
                Id           = c.Id,
                Name         = c.Name,
                Description  = c.Description,
                ThumbnailUrl = c.ThumbnailUrl,
                Level        = c.Level.ToString(),
                DurationHours= c.DurationHours,
                IsPublished  = c.IsPublished,
                IsPublicationLocked = c.IsPublicationLocked,
                CreatedAt    = c.CreatedAt,
                UpdatedAt    = c.UpdatedAt,

                Category = new CategoryDto
                {
                    Id         = c.Category.Id,
                    Name       = c.Category.Name,
                    Description= c.Category.Description,
                    OrderIndex = c.Category.OrderIndex
                },

                Tags = c.CourseTags.Select(t => new TagDto
                {
                    Id   = t.Id,
                    Name = t.Name,
                    Code = t.Code
                }).ToList(),

                TotalModules   = c.Modules.Count,
                TotalMaterials = c.Modules.Sum(m => m.Materials.Count)
            }).ToList();

            return new PageResult<ExpertCourseListItemDto>(
                items,
                totalCount,
                request.PageIndex,
                request.PageSize);
        }
    }
}
