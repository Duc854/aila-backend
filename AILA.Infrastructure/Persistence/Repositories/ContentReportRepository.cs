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

        public async Task<bool> HasPendingCourseReportAsync(Guid learnerId, Guid courseId, CancellationToken cancellationToken = default)
        {
            return await _context.Set<ContentReport>()
                .AsNoTracking()
                .AnyAsync(r => r.LearnerId == learnerId
                               && r.CourseId == courseId
                               && r.Status == ReportStatus.Pending,
                    cancellationToken);
        }
        /// <summary>
        /// UC-79
        /// Lấy danh sách báo cáo, hỗ trợ lọc theo trạng thái và loại nội dung.
        /// </summary>
        public async Task<IEnumerable<ContentReport>> GetReportsAsync(
            ReportStatus? status,
            bool? isCourseReport,
            CancellationToken cancellationToken = default)
        {
            IQueryable<ContentReport> query = _context.Set<ContentReport>()
                .AsNoTracking();

            if (status.HasValue)
            {
                query = query.Where(r => r.Status == status.Value);
            }

            if (isCourseReport.HasValue)
            {
                query = isCourseReport.Value
                    ? query.Where(r => r.CourseId != null)
                    : query.Where(r => r.MaterialId != null);
            }

            return await query
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// UC-79
        /// Lấy chi tiết một báo cáo.
        /// </summary>
        public async Task<ContentReport?> GetReportByIdAsync(
            Guid reportId,
            CancellationToken cancellationToken = default)
        {
            return await _context.Set<ContentReport>()
                .AsNoTracking()
                .Include(r => r.Learner)
                .Include(r => r.Course)
                .Include(r => r.Material)
                .FirstOrDefaultAsync(r => r.Id == reportId, cancellationToken);
        }
    }
}



