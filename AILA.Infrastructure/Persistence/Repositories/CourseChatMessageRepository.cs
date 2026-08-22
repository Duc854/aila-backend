using AILA.Application.Common.Interfaces.Repositories;
using AILA.Domain.Entities;
using AILA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AILA.Infrastructure.Persistence.Repositories;

public class CourseChatMessageRepository : ICourseChatMessageRepository
{
    private readonly ApplicationDbContext _context;

    public CourseChatMessageRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddMessageAsync(
        CourseChatMessage message,
        CancellationToken cancellationToken = default)
    {
        await _context.CourseChatMessages.AddAsync(
            message,
            cancellationToken);
    }

    public async Task<List<CourseChatMessage>>
        GetMessagesBySessionIdAsync(
            Guid sessionId,
            CancellationToken cancellationToken = default)
    {
        return await _context.CourseChatMessages
            .Where(m => m.SessionId == sessionId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<CourseChatMessage>>
        GetRecentMessagesAsync(
            Guid sessionId,
            int count = 6,
            CancellationToken cancellationToken = default)
    {
        var recent = await _context.CourseChatMessages
            .Where(m => m.SessionId == sessionId)
            .OrderByDescending(m => m.CreatedAt)
            .Take(count)
            .ToListAsync(cancellationToken);

        return recent
            .OrderBy(m => m.CreatedAt)
            .ToList();
    }
}