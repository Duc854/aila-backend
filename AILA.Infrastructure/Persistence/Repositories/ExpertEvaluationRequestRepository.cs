using AILA.Application.Common.Interfaces.Repositories;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AILA.Infrastructure.Persistence.Repositories
{
    public class ExpertEvaluationRequestRepository
        : GenericRepository<ExpertEvaluationRequest>,
          IExpertEvaluationRequestRepository
    {
        public ExpertEvaluationRequestRepository(ApplicationDbContext context)
            : base(context)
        {
        }

        public async Task<ExpertEvaluationRequest?> GetByIdWithEvaluationAsync(
            Guid requestId,
            CancellationToken cancellationToken = default)
        {
            return await _context.ExpertEvaluationRequests
                .Include(x => x.ExpertEvaluation)
                .FirstOrDefaultAsync(x => x.Id == requestId, cancellationToken);
        }

        public async Task<bool> HasActiveRequestForAttemptAsync(
            Guid practiceAttemptId,
            CancellationToken cancellationToken = default)
        {
            return await _context.ExpertEvaluationRequests
                .AsNoTracking()
                .AnyAsync(
                    x => x.PracticeAttemptId == practiceAttemptId
                      && x.Status != ExpertEvaluationRequestStatus.Cancelled,
                    cancellationToken);
        }

        public async Task<(IReadOnlyList<ExpertEvaluationRequest> Items, int TotalCount)> GetAssignedPageAsync(
            Guid expertId,
            ExpertEvaluationRequestStatus? status,
            int pageIndex,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            var query = _context.ExpertEvaluationRequests
                .AsNoTracking()
                .Where(x => x.ExpertId == expertId);

            if (status.HasValue)
                query = query.Where(x => x.Status == status.Value);

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .Include(x => x.Learner)
                    .ThenInclude(l => l.User)
                .Include(x => x.PracticeAttempt)
                // Đang chờ chấm lên đầu, trong cùng nhóm thì cũ nhất trước (AC-63.1)
                .OrderBy(x => x.Status == ExpertEvaluationRequestStatus.InProgress ? 0 : 1)
                .ThenBy(x => x.RequestedAt)
                .Skip(pageIndex * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }
    }
}
