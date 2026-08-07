using AILA.Application.Common.Dtos;
using AILA.Application.Common.Interfaces;
using AILA.Domain.Constants;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using MediatR;

namespace AILA.Application.Features.Courses.Commands
{
    public class EditCourseCommandHandler
        : IRequestHandler<EditCourseCommand, CourseManageResultDto>
    {
        private readonly IUnitOfWork _uow;

        public EditCourseCommandHandler(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<CourseManageResultDto> Handle(
            EditCourseCommand request,
            CancellationToken cancellationToken)
        {
            // 1. Lấy course có tracking để EF Core có thể lưu thay đổi
            var course = await _uow.Courses.GetWithTagsForUpdateAsync(request.CourseId, cancellationToken);
            if (course == null)
                throw new InvalidOperationException("Khóa học không tồn tại.");

            // 2. Chỉ Expert sở hữu mới được sửa
            if (course.ExpertId != request.ExpertId)
                throw new UnauthorizedAccessException("Bạn không có quyền chỉnh sửa khóa học này.");

            // 3. Kiểm tra Category tồn tại
            var category = await _uow.Categories.GetByIdAsync(request.CategoryId);
            if (category == null)
                throw new InvalidOperationException("Danh mục không tồn tại.");

            // 4. Parse Level enum
            if (!Enum.TryParse<KnowledgeLevel>(request.Level, true, out var level))
                throw new InvalidOperationException($"Cấp độ '{request.Level}' không hợp lệ. Giá trị hợp lệ: Beginner, Intermediate, Advanced.");

            // 5. Cập nhật thông tin cơ bản qua domain method
            course.UpdateInfo(request.Name, request.CategoryId, level, request.Description, request.ThumbnailUrl);

            // 6. Cập nhật Tags
            var courseTags = new List<Tag>();


            if (request.TagIds.Any())
            {
                var tags = await _uow.Tags
                    .GetPublishedByIdsAsync(
                        request.TagIds,
                        cancellationToken);


                if (tags.Count != request.TagIds.Count)
                {
                    throw new InvalidOperationException(
                        "Một hoặc nhiều tag không tồn tại hoặc chưa được duyệt.");
                }


                courseTags.AddRange(tags);
            }


            // Auto add level tag
            var levelTag = await GetLevelTagAsync(
                level,
                cancellationToken);


            courseTags.Add(levelTag);


            course.AssignTags(
                courseTags
                    .DistinctBy(x => x.Id)
                    .ToList());

            _uow.Courses.Update(course);
            await _uow.SaveChangesAsync(cancellationToken);

            return new CourseManageResultDto
            {
                Id           = course.Id,
                Name         = course.Name,
                Description  = course.Description,
                ThumbnailUrl = course.ThumbnailUrl,
                Level        = course.Level.ToString(),
                DurationHours= course.DurationHours,
                IsPublished  = course.IsPublished,
                CategoryId   = course.CategoryId,
                TagIds       = course.CourseTags.Select(t => t.Id).ToList(),
                CreatedAt    = course.CreatedAt,
                UpdatedAt    = course.UpdatedAt
            };
        }

        private async Task<Tag> GetLevelTagAsync(
            KnowledgeLevel level,
            CancellationToken cancellationToken)
        {
            var code = level switch
            {
                KnowledgeLevel.Beginner
                    => ReservedTagCodes.Beginner,

                KnowledgeLevel.Intermediate
                    => ReservedTagCodes.Intermediate,

                KnowledgeLevel.Advanced
                    => ReservedTagCodes.Advanced,

                _ => throw new ArgumentOutOfRangeException(nameof(level))
            };


            return await _uow.Tags.GetByCodeAsync(
                code,
                cancellationToken)
                ?? throw new InvalidOperationException(
                    $"Không tìm thấy system tag {code}");
        }
    }
}
