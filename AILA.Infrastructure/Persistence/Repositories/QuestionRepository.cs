using AILA.Application.Common.Interfaces.Repositories;
using AILA.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AILA.Infrastructure.Persistence.Repositories;

public class QuestionRepository
    : GenericRepository<Question>,
      IQuestionRepository
{
    public QuestionRepository(
        ApplicationDbContext context)
        : base(context)
    {
    }

    public async Task<List<Question>> GetByQuizIdAsync(
        Guid quizMaterialId,
        CancellationToken ct = default)
    {
        return await _context.Questions
            .Include(x => x.AnswerOptions)
            .Where(x => x.QuizMaterialId == quizMaterialId)
            .OrderBy(x => x.OrderIndex)
            .ToListAsync(ct);
    }

    public async Task<Question?> GetWithQuizAsync(
        Guid questionId,
        CancellationToken ct = default)
    {
        return await _context.Questions
            .Include(x => x.QuizMaterial)
                .ThenInclude(q => q.Material)
                    .ThenInclude(m => m.Module)
                        .ThenInclude(m => m.Course)
            .FirstOrDefaultAsync(
                x => x.Id == questionId,
                ct);
    }

    public async Task<Question?> GetWithAnswersAsync(
        Guid questionId,
        CancellationToken ct = default)
    {
        return await _context.Questions
            .Include(x => x.AnswerOptions)
            .FirstOrDefaultAsync(
                x => x.Id == questionId,
                ct);
    }

    public async Task<Question?> GetWithQuizAndAnswersAsync(
    Guid questionId,
    CancellationToken ct = default)
    {
        return await _context.Questions
            .Include(x => x.AnswerOptions)

            .Include(x => x.QuizMaterial)
                .ThenInclude(q => q.Material)
                    .ThenInclude(m => m.Module)
                        .ThenInclude(m => m.Course)

            .FirstOrDefaultAsync(
                x => x.Id == questionId,
                ct);
    }
}