using AILA.Application.Common.Interfaces.Repositories;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AILA.Infrastructure.Persistence.Repositories
{
    public class SubscriptionRepository
        : GenericRepository<Subscription>, ISubscriptionRepository
    {
        public SubscriptionRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<Subscription?> GetActiveSubscriptionByLearnerIdAsync(
            Guid learnerId,
            CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;

            return await _context.Subscriptions
                .AsNoTracking()
                .Include(s => s.SubscriptionPlan)
                .Where(s => s.LearnerId == learnerId
                            && s.Status == SubscriptionStatus.Active
                            && s.ExpiredAt >= now)
                .OrderByDescending(s => s.ActivatedAt)
                .FirstOrDefaultAsync(cancellationToken);
        }
    }
}
