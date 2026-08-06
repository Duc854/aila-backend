using AILA.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Common.Interfaces.Repositories
{
    public interface IAccountResourceLimitRepository
        : IGenericRepository<AccountResourceLimit>
    {
        Task<AccountResourceLimit?> GetByAccountIdAsync(
            Guid accountId,
            CancellationToken cancellationToken = default);
    }
}
