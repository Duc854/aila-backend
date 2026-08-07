using AILA.Application.Common.Interfaces.Repositories;
using AILA.Application.Features.Profile.Dtos;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
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

    public async Task<int> GetPracticeAttemptsCountInScopeAsync(
        List<Guid> courseIds, 
        DateTime fromDate, 
        DateTime toDate, 
        CancellationToken cancellationToken = default)
    {
        if (courseIds == null || courseIds.Count == 0)
            return 0;

        return await _context.PracticeAttempts
            .AsNoTracking()
            .Where(pa => _context.Enrollments
                            .Where(e => courseIds.Contains(e.CourseId))
                            .Select(e => e.Id)
                            .Contains(pa.EnrollmentId)
                      && pa.CreatedAt >= fromDate
                      && pa.CreatedAt <= toDate)
            .CountAsync(cancellationToken);
    }

    public async Task<List<AiScenarioHistoryItemDto>> GetCompletedScenarioHistoryByLearnerAsync(
        Guid learnerId, CancellationToken cancellationToken = default)
    {
        var rows = await BuildScenarioHistoryQuery(learnerId)
            .ToListAsync(cancellationToken);

        return rows.Select(ToHistoryItem).ToList();
    }

    public async Task<(IEnumerable<AiScenarioHistoryItemDto> Items, int TotalCount)> GetPagedCompletedScenarioHistoryByLearnerAsync(
        Guid learnerId, int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = BuildScenarioHistoryQuery(learnerId);

        var totalCount = await query.CountAsync(cancellationToken);

        var rows = await query
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (rows.Select(ToHistoryItem).ToList(), totalCount);
    }

    /// <summary>
    /// Lượt thực hành AI đã hoàn thành của một Learner, mới nhất trước.
    /// PracticeAttempt không có navigation sang Enrollment/Material nên phải join thủ công.
    /// </summary>
    private IQueryable<ScenarioHistoryRow> BuildScenarioHistoryQuery(Guid learnerId)
    {
        return from attempt in _context.PracticeAttempts.AsNoTracking()
               join enrollment in _context.Enrollments on attempt.EnrollmentId equals enrollment.Id
               join material in _context.Materials on attempt.MaterialId equals material.Id
               where enrollment.LearnerId == learnerId
                     && attempt.Status == PracticeAttemptStatus.Completed
               orderby attempt.CompletedAt descending, attempt.CreatedAt descending
               select new ScenarioHistoryRow(
                   attempt.Id,
                   enrollment.CourseId,
                   enrollment.Course.Name,
                   material.Id,
                   material.Title,
                   material.AIPracticeDetails != null
                       ? material.AIPracticeDetails.Difficulty
                       : (PracticeDifficulty?)null,
                   attempt.FinalScore,
                   attempt.CreatedAt,
                   attempt.CompletedAt);
    }

    private static AiScenarioHistoryItemDto ToHistoryItem(ScenarioHistoryRow r) => new(
        r.AttemptId,
        r.CourseId,
        r.MaterialId,
        r.ScenarioName,
        r.CourseName,
        r.Difficulty?.ToString(),
        r.Score,
        r.StartedAt,
        r.CompletedAt);

    /// <summary>
    /// Bản ghi thô của truy vấn join — giữ Difficulty ở dạng enum để EF không phải
    /// dịch ToString() xuống SQL; việc đổi sang chuỗi làm ở phía client.
    /// </summary>
    private sealed record ScenarioHistoryRow(
        Guid AttemptId,
        Guid CourseId,
        string CourseName,
        Guid MaterialId,
        string ScenarioName,
        PracticeDifficulty? Difficulty,
        decimal? Score,
        DateTime StartedAt,
        DateTime? CompletedAt);
}
