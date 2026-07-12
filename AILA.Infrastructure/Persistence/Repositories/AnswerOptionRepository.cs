using AILA.Application.Common.Interfaces.Repositories;
using AILA.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AILA.Infrastructure.Persistence.Repositories;

public class AnswerOptionRepository
    : GenericRepository<AnswerOption>,
      IAnswerOptionRepository
{
    public AnswerOptionRepository(
        ApplicationDbContext context)
        : base(context)
    {
    }

    public async Task<List<AnswerOption>> GetByQuestionIdAsync(
        Guid questionId,
        CancellationToken ct = default)
    {
        return await _context.AnswerOptions
            .Where(x => x.QuestionId == questionId)
            .OrderBy(x => x.OrderIndex)
            .ToListAsync(ct);
    }

    public async Task<AnswerOption?> GetWithQuestionAsync(
        Guid answerId,
        CancellationToken ct = default)
    {
        return await _context.AnswerOptions
            .Include(x => x.Question)
                .ThenInclude(q => q.QuizMaterial)
                    .ThenInclude(q => q.Material)
                        .ThenInclude(m => m.Module)
                            .ThenInclude(m => m.Course)
            .FirstOrDefaultAsync(
                x => x.Id == answerId,
                ct);
    }
}