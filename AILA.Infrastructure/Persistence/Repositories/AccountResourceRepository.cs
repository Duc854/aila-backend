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

    public async Task<int> GetTodayTokenUsageAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        var startOfDay = DateTime.UtcNow.Date;
        return await _context.AITokenLogs
            .Where(x => x.AccountId == accountId && x.CreatedAt >= startOfDay)
            .SumAsync(x => (int?)(x.PromptTokens + x.CompletionTokens), cancellationToken) ?? 0;
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
