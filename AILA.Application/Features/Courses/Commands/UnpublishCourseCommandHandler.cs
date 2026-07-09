using AILA.Application.Common.Interfaces;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Courses.Commands
{
    public class UnpublishCourseCommandHandler
        : IRequestHandler<UnpublishCourseCommand, ResponseDto<object>>
    {
        private readonly IUnitOfWork _uow;

        public UnpublishCourseCommandHandler(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<ResponseDto<object>> Handle(
            UnpublishCourseCommand request,
            CancellationToken cancellationToken)
        {
            // 1. Lấy course có tracking
            var course = await _uow.Courses.GetWithTagsForUpdateAsync(request.CourseId, cancellationToken);
            if (course == null)
                return ResponseDto<object>.FailResult("COURSE_NOT_FOUND", "Khóa học không tồn tại.");

            // 2. Chỉ Expert sở hữu mới được unpublish
            if (course.ExpertId != request.ExpertId)
                return ResponseDto<object>.FailResult("FORBIDDEN", "Bạn không có quyền hủy xuất bản khóa học này.");

            // 3. Gọi domain method Unpublish
            course.Unpublish();

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
