using AILA.Application.Common.Dtos;
using AILA.Application.Common.Interfaces;
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
            var tags = request.TagIds.Any()
                ? await _uow.Tags.GetByIdsAsync(request.TagIds, cancellationToken)
                : [];
            course.AssignTags(tags);

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
    }
}
