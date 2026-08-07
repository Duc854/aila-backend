using AILA.Application.Common.Interfaces.Repositories;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Infrastructure.Persistence.Repositories
{
    public class ResourceLimitPolicyRepository
        : GenericRepository<ResourceLimitPolicy>, IResourceLimitPolicyRepository
    {
        public ResourceLimitPolicyRepository(
            ApplicationDbContext context)
            : base(context)
        {
        }


        public async Task<ResourceLimitPolicy?> GetByAccountTypeAsync(
            ResourceAccountType accountType,
            CancellationToken cancellationToken = default)
        {
            return await _context.Set<ResourceLimitPolicy>()
                .FirstOrDefaultAsync(
                    x => x.AccountType == accountType,
                    cancellationToken);
        }
    }
}
