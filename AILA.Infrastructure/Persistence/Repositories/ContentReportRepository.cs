using AILA.Application.Common.Interfaces.Repositories;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AILA.Infrastructure.Persistence.Repositories
{
    public class ContentReportRepository : GenericRepository<ContentReport>, IContentReportRepository
    {
        public ContentReportRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<bool> HasPendingReportAsync(
            Guid learnerId,
            Guid? courseId,
            Guid? materialId,
            CancellationToken cancellationToken = default)
        {
            return await _context.Set<ContentReport>()
                .AsNoTracking()
                .AnyAsync(r => r.LearnerId == learnerId
                               && r.CourseId == courseId
                               && r.MaterialId == materialId
                               && r.Status == ReportStatus.Pending,
                    cancellationToken);
        }

        public async Task<IEnumerable<ContentReport>> GetReportsAsync(
            ReportStatus? filterByStatus,
            bool? isCourseReport,
            CancellationToken cancellationToken = default)
        {
            IQueryable<ContentReport> query = _context.Set<ContentReport>()
                .AsNoTracking()
                .Include(r => r.Course)
                .Include(r => r.Material)
                .Include(r => r.Learner)
                    .ThenInclude(l => l.User);

            // BR-02: Filter by status
            if (filterByStatus.HasValue)
            {
                query = query.Where(r => r.Status == filterByStatus.Value);
            }

            // BR-01: Filter by content type (Course or Material)
            if (isCourseReport.HasValue)
            {
                query = isCourseReport.Value
                    ? query.Where(r => r.MaterialId == null) // Course reports have no material
                    : query.Where(r => r.MaterialId != null); // Material reports have a material
            }

            return await query
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<ContentReport?> GetReportWithDetailsAsync(
            Guid reportId,
            CancellationToken cancellationToken = default)
        {
            return await _context.Set<ContentReport>()
                .AsNoTracking()
                .Include(r => r.Learner)
                    .ThenInclude(l => l.User)
                .Include(r => r.Course)
                .Include(r => r.Material)
                .FirstOrDefaultAsync(r => r.Id == reportId, cancellationToken);
        }

        public async Task<ContentReport?> GetReportWithCourseForUpdateAsync(
            Guid reportId,
            CancellationToken cancellationToken = default)
        {
            // Dùng AsTracking (mặc định) để EF Core track thay đổi trên Course
            return await _context.Set<ContentReport>()
                .Include(r => r.Course)
                .FirstOrDefaultAsync(r => r.Id == reportId, cancellationToken);
        }
    }
}