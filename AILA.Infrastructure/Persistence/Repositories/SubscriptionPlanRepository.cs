using AILA.Application.Common.Interfaces.Repositories;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AILA.Infrastructure.Persistence.Repositories
{
    public class SubscriptionPlanRepository
        : GenericRepository<SubscriptionPlan>, ISubscriptionPlanRepository
    {
        public SubscriptionPlanRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<SubscriptionPlan>> GetActivePlansOrderedAsync(
            CancellationToken cancellationToken = default)
        {
            return await _context.SubscriptionPlans
                .AsNoTracking()
                .Where(p => p.Status == SubscriptionPlanStatus.Active)
                .OrderBy(p => p.DisplayOrder)
                .ThenBy(p => p.TierLevel)
                .ThenBy(p => p.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<SubscriptionPlan?> GetByIdReadOnlyAsync(
            Guid planId,
            CancellationToken cancellationToken = default)
        {
            return await _context.SubscriptionPlans
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == planId, cancellationToken);
        }

        public async Task<IEnumerable<SubscriptionPlan>> GetAllOrderedAsync(
            CancellationToken cancellationToken = default)
        {
            return await _context.SubscriptionPlans
                .AsNoTracking()
                .OrderBy(p => p.DisplayOrder)
                .ThenBy(p => p.TierLevel)
                .ThenBy(p => p.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> ExistsByNameAsync(
            string name,
            CancellationToken cancellationToken = default)
        {
            var normalized = name.Trim().ToLower();

            return await _context.SubscriptionPlans
                .AsNoTracking()
                .AnyAsync(p => p.Name.ToLower() == normalized, cancellationToken);
        }

        public async Task<bool> ExistsByTierLevelAsync(
            int tierLevel,
            CancellationToken cancellationToken = default)
        {
            return await _context.SubscriptionPlans
                .AsNoTracking()
                .AnyAsync(p => p.TierLevel == tierLevel, cancellationToken);
        }
    }
}
