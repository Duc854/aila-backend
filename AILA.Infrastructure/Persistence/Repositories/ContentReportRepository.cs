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
    }
}
