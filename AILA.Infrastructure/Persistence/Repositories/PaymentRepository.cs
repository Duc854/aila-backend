using AILA.Application.Common.Interfaces.Repositories;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AILA.Infrastructure.Persistence.Repositories
{
    public class PaymentRepository : GenericRepository<Payment>, IPaymentRepository
    {
        public PaymentRepository(ApplicationDbContext context) : base(context)
        {
        }

        /// <inheritdoc/>
        public async Task<Payment?> GetPendingPaymentByLearnerIdAsync(
            Guid learnerId,
            CancellationToken cancellationToken = default)
        {
            return await _context.Payments
                .Where(p => p.LearnerId == learnerId
                         && p.Status == PaymentStatus.Pending)
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<Payment?> GetByOrderCodeAsync(
            string orderCode,
            CancellationToken cancellationToken = default)
        {
            return await _context.Payments
                .FirstOrDefaultAsync(p => p.OrderCode == orderCode, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<(IEnumerable<Payment> Items, int TotalCount)> GetPaymentHistoryAsync(
            Guid learnerId,
            DateTime? fromDate,
            DateTime? toDate,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            var query = _context.Payments
                .AsNoTracking()
                .Include(p => p.SubscriptionPlan)
                .Where(p => p.LearnerId == learnerId);

            // BR-02: Filter theo date range (dựa trên CreatedAt)
            if (fromDate.HasValue)
                query = query.Where(p => p.CreatedAt >= fromDate.Value.ToUniversalTime());

            if (toDate.HasValue)
            {
                // Lấy hết ngày toDate (23:59:59)
                var endOfDay = toDate.Value.ToUniversalTime().Date.AddDays(1).AddTicks(-1);
                query = query.Where(p => p.CreatedAt <= endOfDay);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }

        /// <inheritdoc/>
        public async Task<Payment?> GetPaymentDetailAsync(
            Guid paymentId,
            Guid learnerId,
            CancellationToken cancellationToken = default)
        {
            return await _context.Payments
                .AsNoTracking()
                .Include(p => p.SubscriptionPlan)
                .FirstOrDefaultAsync(
                    p => p.Id == paymentId && p.LearnerId == learnerId,
                    cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<AILA.Application.Features.Subscriptions.Dtos.SubscriptionStatisticsDto> GetSubscriptionStatisticsAsync(
            DateTime? fromDate,
            DateTime? toDate,
            CancellationToken cancellationToken = default)
        {
            var paymentsQuery = _context.Payments
                .AsNoTracking()
                .Where(p => p.Status == PaymentStatus.Success);

            if (fromDate.HasValue)
            {
                var startUtc = fromDate.Value.ToUniversalTime();
                paymentsQuery = paymentsQuery.Where(p => (p.PaidAt ?? p.CreatedAt) >= startUtc);
            }

            if (toDate.HasValue)
            {
                var endUtc = toDate.Value.ToUniversalTime().Date.AddDays(1).AddTicks(-1);
                paymentsQuery = paymentsQuery.Where(p => (p.PaidAt ?? p.CreatedAt) <= endUtc);
            }

            var successfulPayments = await paymentsQuery.ToListAsync(cancellationToken);

            var totalRevenue = successfulPayments.Sum(p => p.Amount);
            var totalTransactions = successfulPayments.Count;
            var totalUniqueBuyers = successfulPayments.Select(p => p.LearnerId).Distinct().Count();

            var now = DateTime.UtcNow;
            var activeSubscriptionsCount = await _context.Subscriptions
                .AsNoTracking()
                .CountAsync(s => s.Status == SubscriptionStatus.Active && s.ExpiredAt > now, cancellationToken);

            var expiredSubscriptionsCount = await _context.Subscriptions
                .AsNoTracking()
                .CountAsync(s => s.Status == SubscriptionStatus.Expired || s.ExpiredAt <= now, cancellationToken);

            var plans = await _context.SubscriptionPlans
                .AsNoTracking()
                .OrderBy(p => p.DisplayOrder)
                .ThenBy(p => p.TierLevel)
                .ToListAsync(cancellationToken);

            var activeSubByPlan = await _context.Subscriptions
                .AsNoTracking()
                .Where(s => s.Status == SubscriptionStatus.Active && s.ExpiredAt > now)
                .GroupBy(s => s.SubscriptionPlanId)
                .Select(g => new { PlanId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.PlanId, x => x.Count, cancellationToken);

            var planBreakdowns = plans.Select(plan =>
            {
                var paymentsForPlan = successfulPayments.Where(p => p.SubscriptionPlanId == plan.Id).ToList();
                return new AILA.Application.Features.Subscriptions.Dtos.SubscriptionPlanStatDto
                {
                    PlanId = plan.Id,
                    PlanName = plan.Name,
                    TierLevel = plan.TierLevel,
                    CurrentPrice = plan.Price,
                    TotalPurchases = paymentsForPlan.Count,
                    TotalRevenue = paymentsForPlan.Sum(p => p.Amount),
                    ActiveCount = activeSubByPlan.TryGetValue(plan.Id, out var count) ? count : 0
                };
            }).ToList();

            var revenueTrends = successfulPayments
                .GroupBy(p => (p.PaidAt ?? p.CreatedAt).ToString("yyyy-MM-dd"))
                .OrderBy(g => g.Key)
                .Select(g => new AILA.Application.Features.Subscriptions.Dtos.SubscriptionRevenueTrendDto
                {
                    Date = g.Key,
                    Revenue = g.Sum(p => p.Amount),
                    TransactionCount = g.Count()
                })
                .ToList();

            return new AILA.Application.Features.Subscriptions.Dtos.SubscriptionStatisticsDto
            {
                TotalRevenue = totalRevenue,
                TotalTransactions = totalTransactions,
                TotalUniqueBuyers = totalUniqueBuyers,
                ActiveSubscriptionsCount = activeSubscriptionsCount,
                ExpiredSubscriptionsCount = expiredSubscriptionsCount,
                PlanBreakdowns = planBreakdowns,
                RevenueTrends = revenueTrends
            };
        }
    }
}
