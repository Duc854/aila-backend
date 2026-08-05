using AILA.Application.Common.Interfaces.Repositories;
using AILA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AILA.Infrastructure.Persistence.Repositories;

public class PracticeAttemptRepository : IPracticeAttemptRepository
{
    private readonly ApplicationDbContext _context;

    public PracticeAttemptRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PracticeAttempt?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.PracticeAttempts
            .Include(x => x.Submissions)
                .ThenInclude(s => s.CriteriaScores)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<PracticeAttempt?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.PracticeAttempts
            .Include(a => a.Submissions)
                .ThenInclude(s => s.CriteriaScores)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<List<PracticeAttempt>> GetByEnrollmentIdAsync(Guid enrollmentId, CancellationToken cancellationToken = default)
    {
        return await _context.PracticeAttempts
            .Include(a => a.Submissions)
                .ThenInclude(s => s.CriteriaScores)
            .Where(x => x.EnrollmentId == enrollmentId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(PracticeAttempt attempt, CancellationToken cancellationToken = default)
    {
        await _context.PracticeAttempts.AddAsync(attempt, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(PracticeAttempt attempt, CancellationToken cancellationToken = default)
    {
        _context.PracticeAttempts.Update(attempt);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
