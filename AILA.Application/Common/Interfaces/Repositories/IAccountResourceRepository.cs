using AILA.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AILA.Application.Common.Interfaces.Repositories;

public interface IAccountResourceRepository
{
    Task<int> GetTodayTokenUsageAsync(Guid accountId, CancellationToken cancellationToken = default);
    Task AddTokenLogAsync(AITokenLog log, CancellationToken cancellationToken = default);
    Task<List<AITokenLog>> GetTokenLogsForAccountAsync(Guid accountId, CancellationToken cancellationToken = default);
}
