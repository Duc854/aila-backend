using AILA.Domain.Entities;

namespace AILA.Application.Common.Interfaces.Repositories;

public interface IQuestionRepository
    : IGenericRepository<Question>
{
    /// <summary>
    /// Lấy toàn bộ Question của một Quiz.
    /// Dùng cho màn hình Manage Quiz.
    /// </summary>
    Task<List<Question>> GetByQuizIdAsync(
        Guid quizMaterialId,
        CancellationToken ct = default);

    /// <summary>
    /// Lấy Question kèm QuizMaterial và Material để kiểm tra quyền Expert.
    /// </summary>
    Task<Question?> GetWithQuizAsync(
        Guid questionId,
        CancellationToken ct = default);

    /// <summary>
    /// Lấy Question kèm toàn bộ AnswerOption.
    /// </summary>
    Task<Question?> GetWithAnswersAsync(
        Guid questionId,
        CancellationToken ct = default);

    Task<Question?> GetWithQuizAndAnswersAsync(
    Guid questionId,
    CancellationToken ct = default);
}