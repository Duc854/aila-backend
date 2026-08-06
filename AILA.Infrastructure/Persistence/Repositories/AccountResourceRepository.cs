using AILA.Application.Common.Interfaces.Repositories;
using AILA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AILA.Infrastructure.Persistence.Repositories;

public class AccountResourceRepository : IAccountResourceRepository
{
    private readonly ApplicationDbContext _context;

    public AccountResourceRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UserTokenQuota?> GetQuotaAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        return await _context.UserTokenQuotas
            .FirstOrDefaultAsync(x => x.AccountId == accountId, cancellationToken);
    }

    public async Task AddQuotaAsync(UserTokenQuota quota, CancellationToken cancellationToken = default)
    {
        await _context.UserTokenQuotas.AddAsync(quota, cancellationToken);
    }

    public async Task AddTokenLogAsync(AITokenLog log, CancellationToken cancellationToken = default)
    {
        await _context.AITokenLogs.AddAsync(log, cancellationToken);
    }

    public async Task<List<AITokenLog>> GetTokenLogsForAccountAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        return await _context.AITokenLogs
            .Where(x => x.AccountId == accountId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
