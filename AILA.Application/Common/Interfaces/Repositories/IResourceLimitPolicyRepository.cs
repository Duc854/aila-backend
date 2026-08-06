using AILA.Domain.Entities;
using AILA.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Common.Interfaces.Repositories
{
    public interface IResourceLimitPolicyRepository
        : IGenericRepository<ResourceLimitPolicy>
    {
        Task<ResourceLimitPolicy?> GetByAccountTypeAsync(
            ResourceAccountType accountType,
            CancellationToken cancellationToken = default);
    }
}
