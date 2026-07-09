using AILA.Application.Common.Interfaces;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Courses.Commands
{
    public class PublishCourseCommandHandler
        : IRequestHandler<PublishCourseCommand, ResponseDto<object>>
    {
        private readonly IUnitOfWork _uow;

        public PublishCourseCommandHandler(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<ResponseDto<object>> Handle(
            PublishCourseCommand request,
            CancellationToken cancellationToken)
        {
            // 1. Lấy course có tracking để lưu thay đổi
            var course = await _uow.Courses.GetWithTagsForUpdateAsync(request.CourseId, cancellationToken);
            if (course == null)
                return ResponseDto<object>.FailResult("COURSE_NOT_FOUND", "Khóa học không tồn tại.");

            // 2. Chỉ Expert sở hữu mới được publish
            if (course.ExpertId != request.ExpertId)
                return ResponseDto<object>.FailResult("FORBIDDEN", "Bạn không có quyền xuất bản khóa học này.");

            // 3. Gọi domain method Publish (có business rule: phải có ít nhất 1 module)
            try
            {
                course.Publish();
            }
            catch (InvalidOperationException ex)
            {
                return ResponseDto<object>.FailResult("PUBLISH_FAILED", ex.Message);
            }

            _uow.Courses.Update(course);
            await _uow.SaveChangesAsync(cancellationToken);

            return ResponseDto<object>.SuccessResult(new
            {
                CourseId = course.Id,
                IsPublished = course.IsPublished
            });
        }
    }
}
