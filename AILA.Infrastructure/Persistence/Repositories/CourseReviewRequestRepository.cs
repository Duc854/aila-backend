using AILA.Application.Common.Interfaces.Repositories;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AILA.Infrastructure.Persistence.Repositories;

public sealed class CourseReviewRequestRepository
    : GenericRepository<CourseReviewRequest>, ICourseReviewRequestRepository
{
    public CourseReviewRequestRepository(ApplicationDbContext context) : base(context) { }

    public async Task<bool> HasPendingRequestAsync(Guid courseId, CancellationToken ct = default)
        => await _context.CourseReviewRequests
            .AnyAsync(r => r.CourseId == courseId
                        && r.Status == CourseReviewRequestStatus.Pending, ct);

    public async Task<CourseReviewRequest?> GetWithCourseAsync(Guid requestId, CancellationToken ct = default)
        => await _context.CourseReviewRequests
            .Include(r => r.Course)
                .ThenInclude(c => c.Expert)
                    .ThenInclude(e => e.User)
            .FirstOrDefaultAsync(r => r.Id == requestId, ct);

    public async Task<List<CourseReviewRequest>> GetAllWithDetailsAsync(
        CourseReviewRequestStatus? status,
        CancellationToken ct = default)
    {
        var query = _context.CourseReviewRequests
            .Include(r => r.Course)
                .ThenInclude(c => c.Expert)
                    .ThenInclude(e => e.User)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);

        return await query
            .OrderByDescending(r => r.CreatedAt)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<List<CourseReviewRequest>> GetByExpertAsync(Guid expertId, CancellationToken ct = default)
        => await _context.CourseReviewRequests
            .Include(r => r.Course)
            .Where(r => r.Course.ExpertId == expertId)
            .OrderByDescending(r => r.CreatedAt)
            .AsNoTracking()
            .ToListAsync(ct);
}
