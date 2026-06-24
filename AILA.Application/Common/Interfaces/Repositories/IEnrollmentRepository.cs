using AILA.Domain.Entities;

namespace AILA.Application.Common.Interfaces.Repositories
{
    public interface IEnrollmentRepository : IGenericRepository<Enrollment>
    {
        /// Kiểm tra học viên đã tham gia khóa học này chưa
        Task<Enrollment?> GetByLearnerAndCourseAsync(Guid learnerId, Guid courseId);

        Task<Enrollment?> GetByCourseAndLearnerAsync(Guid courseId, Guid learnerId, CancellationToken cancellationToken = default);
        void Update(Enrollment enrollment);
    }
}
