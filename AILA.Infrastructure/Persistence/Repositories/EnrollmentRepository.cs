using AILA.Application.Common.Interfaces.Repositories;
using AILA.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AILA.Infrastructure.Persistence.Repositories
{
    public class EnrollmentRepository : GenericRepository<Enrollment>, IEnrollmentRepository
    {
        public EnrollmentRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<Enrollment?> GetByLearnerAndCourseAsync(Guid learnerId, Guid courseId)
        {
            return await _context.Enrollments
                .FirstOrDefaultAsync(e => e.LearnerId == learnerId && e.CourseId == courseId);
        }

        public async Task<Enrollment?> GetByCourseAndLearnerAsync(Guid courseId, Guid learnerId, CancellationToken cancellationToken = default)
        {
            return await _context.Enrollments
                .FirstOrDefaultAsync(e => e.CourseId == courseId && e.LearnerId == learnerId, cancellationToken);
        }

        public void Update(Enrollment enrollment)
        {
            _context.Enrollments.Update(enrollment);
        }
    }
}
