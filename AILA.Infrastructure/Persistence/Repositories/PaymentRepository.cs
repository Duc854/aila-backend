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
    }
}
