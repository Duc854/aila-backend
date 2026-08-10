using AILA.Application.Common.Interfaces.Repositories;
using AILA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Infrastructure.Persistence.Repositories
{
    public class AccountResourceLimitRepository
        : GenericRepository<AccountResourceLimit>,
          IAccountResourceLimitRepository
    {
        private readonly ApplicationDbContext _context;


        public AccountResourceLimitRepository(
            ApplicationDbContext context)
            : base(context)
        {
            _context = context;
        }


        public async Task<AccountResourceLimit?> GetByAccountIdAsync(
            Guid accountId,
            CancellationToken cancellationToken = default)
        {
            return await _context.AccountResourceLimits
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.AccountId == accountId,
                    cancellationToken);
        }
    }
}
