using AILA.Domain.Entities;

namespace AILA.Application.Common.Interfaces.Repositories;

public interface IAnswerOptionRepository
    : IGenericRepository<AnswerOption>
{
    /// <summary>
    /// Lấy toàn bộ Answer của một Question.
    /// </summary>
    Task<List<AnswerOption>> GetByQuestionIdAsync(
        Guid questionId,
        CancellationToken ct = default);

    /// <summary>
    /// Lấy Answer kèm Question -> Quiz -> Material -> Module -> Course.
    /// Dùng để kiểm tra quyền Expert.
    /// </summary>
    Task<AnswerOption?> GetWithQuestionAsync(
        Guid answerId,
        CancellationToken ct = default);
}