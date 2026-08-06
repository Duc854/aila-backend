using AILA.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AILA.Application.Common.Interfaces.Repositories;

public interface IAccountResourceRepository
{
    Task<UserTokenQuota?> GetQuotaAsync(Guid accountId, CancellationToken cancellationToken = default);
    Task AddQuotaAsync(UserTokenQuota quota, CancellationToken cancellationToken = default);
    Task AddTokenLogAsync(AITokenLog log, CancellationToken cancellationToken = default);
    Task<List<AITokenLog>> GetTokenLogsForAccountAsync(Guid accountId, CancellationToken cancellationToken = default);
}
