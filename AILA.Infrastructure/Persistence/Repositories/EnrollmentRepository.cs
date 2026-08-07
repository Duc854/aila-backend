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

        public async Task<bool> HasEnrollmentsForCourseAsync(Guid courseId, CancellationToken cancellationToken = default)
            => await _context.Enrollments.AnyAsync(e => e.CourseId == courseId, cancellationToken);

        public async Task<List<Enrollment>> GetEnrollmentsWithCourseByLearnerIdAsync(Guid learnerId, CancellationToken ct = default)
            => await _context.Enrollments
                .AsNoTracking()
                .Include(e => e.Course)
                    .ThenInclude(c => c.Category)
                .Where(e => e.LearnerId == learnerId)
                .OrderByDescending(e => e.LastAccessedAt ?? DateTime.MinValue)
                .ThenByDescending(e => e.EnrolledAt)
                .ToListAsync(ct);

        public async Task<(IEnumerable<Enrollment> Items, int TotalCount)> GetPagedEnrollmentsByLearnerAsync(
            Guid learnerId, int pageIndex, int pageSize, CancellationToken ct = default)
        {
            var query = _context.Enrollments
                .AsNoTracking()
                .Include(e => e.Course)
                    .ThenInclude(c => c.Category)
                .Where(e => e.LearnerId == learnerId);

            var totalCount = await query.CountAsync(ct);

            var items = await query
                .OrderByDescending(e => e.LastAccessedAt ?? DateTime.MinValue)
                .ThenByDescending(e => e.EnrolledAt)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return (items, totalCount);
        }

        public void Update(Enrollment enrollment)
        {
            _context.Enrollments.Update(enrollment);
        }

        public async Task<List<Enrollment>> GetEnrollmentsInScopeAsync(
            List<Guid> courseIds, 
            DateTime fromDate, 
            DateTime toDate, 
            CancellationToken ct = default)
        {
            if (courseIds == null || courseIds.Count == 0)
                return new List<Enrollment>();

            return await _context.Enrollments
                .AsNoTracking()
                .Where(e => courseIds.Contains(e.CourseId) 
                         && e.EnrolledAt >= fromDate 
                         && e.EnrolledAt <= toDate)
                .ToListAsync(ct);
        }
    }
}
