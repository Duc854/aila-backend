using AILA.Application.Common.Interfaces.Repositories;
using AILA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AILA.Infrastructure.Persistence.Repositories
{
    public class AccountResourceUsageRepository 
        : GenericRepository<AccountResourceUsage>, IAccountResourceUsageRepository
    {
        public AccountResourceUsageRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<AccountResourceUsage?> GetByAccountIdAsync(
            Guid accountId, 
            CancellationToken cancellationToken = default)
        {
            return await _context.AccountResourceUsages
                .AsNoTracking()
                .Where(u => u.AccountId == accountId)
                .OrderByDescending(u => u.PeriodEnd)
                .FirstOrDefaultAsync(cancellationToken);
        }
    }
}
