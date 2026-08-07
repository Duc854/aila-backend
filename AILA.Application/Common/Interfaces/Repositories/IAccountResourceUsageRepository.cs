using AILA.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AILA.Application.Common.Interfaces.Repositories
{
    public interface IAccountResourceUsageRepository : IGenericRepository<AccountResourceUsage>
    {
        Task<AccountResourceUsage?> GetByAccountIdAsync(
            Guid accountId, 
            CancellationToken cancellationToken = default);
    }
}
