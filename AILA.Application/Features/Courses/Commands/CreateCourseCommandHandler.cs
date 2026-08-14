using AILA.Application.Common.Dtos;
using AILA.Application.Common.Interfaces;
using AILA.Domain.Constants;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using MediatR;

namespace AILA.Application.Features.Courses.Commands
{
    public class CreateCourseCommandHandler
        : IRequestHandler<CreateCourseCommand, CourseManageResultDto>
    {
        private readonly IUnitOfWork _uow;

        public CreateCourseCommandHandler(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<CourseManageResultDto> Handle(
            CreateCourseCommand request,
            CancellationToken cancellationToken)
        {
            // 1. Kiểm tra Expert tồn tại (Expert dùng UserId làm PK, không có Id riêng)
            var expert = await _uow.Experts.GetReadonlyWithUserAsync(request.ExpertId, cancellationToken);
            if (expert == null)
                throw new InvalidOperationException($"Chuyên gia không tồn tại. (ExpertId nhận được: {request.ExpertId})");

            // 2. Kiểm tra Category tồn tại
            var category = await _uow.Categories.GetByIdAsync(request.CategoryId);
            if (category == null)
                throw new InvalidOperationException("Danh mục không tồn tại.");

            // 3. Parse Level enum
            if (!Enum.TryParse<KnowledgeLevel>(request.Level, true, out var level))
                throw new InvalidOperationException($"Cấp độ '{request.Level}' không hợp lệ. Giá trị hợp lệ: Beginner, Intermediate, Advanced.");

            // 4. Tạo Course mới theo DDD constructor
            var course = new Course(
                request.Name,
                request.CategoryId,
                request.ExpertId,
                level,
                request.Description,
                request.ThumbnailUrl);

            // 5. Cập nhật duration nếu được cung cấp
            if (request.DurationHours > 0)
            {
                course.UpdateDuration(request.DurationHours);
            }

            // 6. Gán Tags nếu có
            var allTagIds = new List<Guid>(request.TagIds);

            // System auto tag theo level - get level tag ID
            var levelTagCode = level switch
            {
                KnowledgeLevel.Beginner => ReservedTagCodes.Beginner,
                KnowledgeLevel.Intermediate => ReservedTagCodes.Intermediate,
                KnowledgeLevel.Advanced => ReservedTagCodes.Advanced,
                _ => throw new ArgumentOutOfRangeException(nameof(level))
            };

            var levelTag = await _uow.Tags.GetByCodeAsync(levelTagCode, cancellationToken);
            if (levelTag == null)
                throw new InvalidOperationException($"Không tìm thấy level tag: {levelTagCode}");

            // Add level tag ID if not already in the list
            if (!allTagIds.Contains(levelTag.Id))
            {
                allTagIds.Add(levelTag.Id);
            }

            // Load all tags in a single query to avoid tracking conflicts
            var courseTags = new List<Tag>();
            if (allTagIds.Any())
            {
                var tags = await _uow.Tags.GetByIdsAsync(allTagIds, cancellationToken);

                // Validate: Tất cả tags phải tồn tại
                if (tags.Count != allTagIds.Count)
                {
                    throw new InvalidOperationException("Một hoặc nhiều tag không tồn tại.");
                }

                courseTags.AddRange(tags);
            }

            course.AssignTags(courseTags);

            await _uow.Courses.AddAsync(course);
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
