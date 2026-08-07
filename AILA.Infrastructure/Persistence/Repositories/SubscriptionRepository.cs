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

        public async Task<AccountResourceUsage?> GetResourceUsageByAccountIdAsync(
            Guid accountId,
            CancellationToken cancellationToken = default)
        {
            return await _context.AccountResourceUsages
                .AsNoTracking()
                .Where(u => u.AccountId == accountId)
                .OrderByDescending(u => u.PeriodEnd)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<AccountResourceLimit?> GetResourceLimitByAccountIdAsync(
            Guid accountId,
            CancellationToken cancellationToken = default)
        {
            return await _context.AccountResourceLimits
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.AccountId == accountId, cancellationToken);
        }

        public async Task<ResourceLimitPolicy?> GetDefaultPolicyAsync(
            ResourceAccountType accountType = ResourceAccountType.Learner,
            CancellationToken cancellationToken = default)
        {
            return await _context.ResourceLimitPolicies
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.AccountType == accountType, cancellationToken);
        }
    }
}
