using AILA.Application.Common.Dtos;
using AILA.Application.Common.Interfaces;
using AILA.Domain.Constants;
using AILA.Domain.Entities;
using MediatR;

namespace AILA.Application.Features.Courses.Commands
{
    public class EnrollCourseCommandHandler
        : IRequestHandler<EnrollCourseCommand, EnrollmentResultDto>
    {
        private readonly IUnitOfWork _uow;
        private readonly ILearnerBehaviorService _behaviorService;


        public EnrollCourseCommandHandler(
            IUnitOfWork uow,
            ILearnerBehaviorService behaviorService)
        {
            _uow = uow;
            _behaviorService = behaviorService;
        }

        public async Task<EnrollmentResultDto> Handle(
            EnrollCourseCommand request,
            CancellationToken cancellationToken)
        {
            // 1. Kiểm tra khóa học tồn tại và đã công khai
            var course = await _uow.Courses
                .GetWithTagsAsync(
                    request.CourseId,
                    cancellationToken);
            if (course == null)
                throw new InvalidOperationException("Khóa học không tồn tại.");
            if (!course.IsPublished)
                throw new InvalidOperationException("Khóa học chưa được công khai.");

            // 2. Kiểm tra học viên tồn tại
            var learner = await _uow.Repository<Learner>().GetByIdAsync(request.LearnerId);
            if (learner == null)
                throw new InvalidOperationException("Học viên không tồn tại.");

            // 3. Kiểm tra đã tham gia chưa
            var existing = await _uow.Enrollments
                .GetByLearnerAndCourseAsync(request.LearnerId, request.CourseId);
            if (existing != null)
                throw new InvalidOperationException("Bạn đã tham gia khóa học này rồi.");

            // 4. Đếm tổng số bài học của khóa học
            var totalMaterials = await _uow.Courses.CountMaterialsAsync(request.CourseId);
            await _uow.BeginTransactionAsync(cancellationToken);

            try
            {
                // 5. Tạo enrollment
                var enrollment = new Enrollment(
                    request.LearnerId,
                    request.CourseId,
                    totalMaterials);


                await _uow.Enrollments.AddAsync(enrollment);



                // 6. Chỉ lấy interest tags để cộng behavior
                var behaviorTags = course.CourseTags
                    .Where(t =>
                        !ReservedTagCodes.LevelTags.Contains(t.Code)
                        &&
                        !ReservedTagCodes.LearnerTypeTags.Contains(t.Code))
                    .ToList();



                if (behaviorTags.Any())
                {
                    await _behaviorService.IncreaseScoreAsync(
                        request.LearnerId,
                        behaviorTags,
                        BehaviorScoreConstants.EnrollCourse,
                        cancellationToken);
                }



                // 7. Commit transaction
                await _uow.CommitTransactionAsync(
                    cancellationToken);



                return new EnrollmentResultDto
                {
                    EnrollmentId = enrollment.Id,
                    CourseId = enrollment.CourseId,
                    LearnerId = enrollment.LearnerId,
                    Status = enrollment.Status.ToString(),
                    EnrolledAt = enrollment.EnrolledAt
                };
            }
            catch
            {
                await _uow.RollbackTransactionAsync(
                    cancellationToken);

                throw;
            }
        }
    }
}
