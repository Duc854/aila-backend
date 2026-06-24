using AILA.Application.Common.Dtos;
using AILA.Application.Common.Interfaces;
using AILA.Domain.Entities;
using MediatR;

namespace AILA.Application.Features.Courses.Commands
{
    public class EnrollCourseCommandHandler
        : IRequestHandler<EnrollCourseCommand, EnrollmentResultDto>
    {
        private readonly IUnitOfWork _uow;

        public EnrollCourseCommandHandler(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<EnrollmentResultDto> Handle(
            EnrollCourseCommand request,
            CancellationToken cancellationToken)
        {
            // 1. Kiểm tra khóa học tồn tại và đã công khai
            var course = await _uow.Courses.GetByIdAsync(request.CourseId);
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

            // 5. Tạo Enrollment mới theo DDD constructor
            var enrollment = new Enrollment(request.LearnerId, request.CourseId, totalMaterials);

            await _uow.Enrollments.AddAsync(enrollment);
            await _uow.SaveChangesAsync(cancellationToken);

            return new EnrollmentResultDto
            {
                EnrollmentId = enrollment.Id,
                CourseId = enrollment.CourseId,
                LearnerId = enrollment.LearnerId,
                Status = enrollment.Status.ToString(),
                EnrolledAt = enrollment.EnrolledAt
            };
        }
    }
}
