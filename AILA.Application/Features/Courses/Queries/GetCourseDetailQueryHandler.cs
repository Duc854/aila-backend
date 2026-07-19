using AILA.Application.Common.Dtos;
using AILA.Application.Common.Interfaces;
using MediatR;

namespace AILA.Application.Features.Courses.Queries
{
    public class GetCourseDetailQueryHandler
        : IRequestHandler<GetCourseDetailQuery, CourseDetailDto?>
    {
        private readonly IUnitOfWork _uow;

        public GetCourseDetailQueryHandler(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<CourseDetailDto?> Handle(
            GetCourseDetailQuery request,
            CancellationToken cancellationToken)
        {
            var course = await _uow.Courses.GetCourseDetailAsync(request.CourseId);

            if (course == null)
                return null;

            var modules = course.Modules
                .OrderBy(m => m.OrderIndex)
                .Select(m => new ModuleDetailDto
                {
                    Id = m.Id,
                    Title = m.Title,
                    Description = m.Description,
                    OrderIndex = m.OrderIndex,
                    Materials = m.Materials
                        .OrderBy(mat => mat.OrderIndex)
                        .Select(mat => new MaterialDetailDto
                        {
                            Id           = mat.Id,
                            ModuleId     = mat.ModuleId,
                            Title        = mat.Title,
                            Type = mat.MaterialType.ToString(),
                            OrderIndex   = mat.OrderIndex,
                            VideoDetails = mat.VideoDetails == null ? null : new VideoMaterialDto
                            {
                                VideoUrl     = mat.VideoDetails.VideoUrl,
                                Content = mat.VideoDetails.Content,
                            },
                            DocumentDetails = mat.DocumentDetails == null ? null : new DocumentMaterialDto
                            {
                                Content = mat.DocumentDetails.Content,
                            }
                        }).ToList()
                }).ToList();

            var totalMaterials = modules.Sum(m => m.Materials.Count);

            return new CourseDetailDto
            {
                Id = course.Id,
                Name = course.Name,
                Description = course.Description,
                ThumbnailUrl = course.ThumbnailUrl,
                Level = course.Level.ToString(),
                DurationHours = course.DurationHours,
                IsPublished = course.IsPublished,

                Category = new CategoryDto
                {
                    Id = course.Category.Id,
                    Name = course.Category.Name,
                    Description = course.Category.Description,
                    OrderIndex = course.Category.OrderIndex
                },

                Author = new AuthorDto
                {
                    UserId = course.Expert.UserId,
                    FullName = course.Expert.User.FullName,
                    AvatarUrl = course.Expert.User.AvatarUrl,
                    Specialty = course.Expert.Specialty,
                    Bio = course.Expert.Bio,
                    YearsOfExperience = course.Expert.YearsOfExperience
                },

                Tags = course.CourseTags.Select(t => new TagDto
                {
                    Id = t.Id,
                    Name = t.Name,
                    Code = t.Code
                }).ToList(),

                Modules = modules,
                TotalModules = modules.Count,
                TotalMaterials = totalMaterials
            };
        }
    }
}
