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

public class CourseChatSessionRepository : ICourseChatSessionRepository
{
    private readonly ApplicationDbContext _context;

    public CourseChatSessionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CourseChatSession?> GetSessionByIdAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        return await _context.CourseChatSessions
            .FirstOrDefaultAsync(
                s => s.Id == sessionId,
                cancellationToken);
    }

    public async Task<List<CourseChatSession>>
        GetSessionsByAccountAndCourseAsync(
            Guid accountId,
            Guid courseId,
            CancellationToken cancellationToken = default)
    {
        return await _context.CourseChatSessions
            .Where(s =>
                s.AccountId == accountId &&
                s.CourseId == courseId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddSessionAsync(
        CourseChatSession session,
        CancellationToken cancellationToken = default)
    {
        await _context.CourseChatSessions.AddAsync(
            session,
            cancellationToken);
    }
}