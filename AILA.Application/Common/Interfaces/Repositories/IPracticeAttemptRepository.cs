// Application/Common/Interfaces/Repositories/IPracticeAttemptRepository.cs
using AILA.Application.Features.Profile.Dtos;
using AILA.Domain.Entities;

namespace AILA.Application.Common.Interfaces.Repositories;

public interface IPracticeAttemptRepository
{
    Task<PracticeAttempt?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PracticeAttempt?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<PracticeAttempt>> GetByEnrollmentIdAsync(Guid enrollmentId, CancellationToken cancellationToken = default);
    Task AddAsync(PracticeAttempt attempt, CancellationToken cancellationToken = default);
    Task UpdateAsync(PracticeAttempt attempt, CancellationToken cancellationToken = default);
    Task<int> GetPracticeAttemptsCountInScopeAsync(
        List<Guid> courseIds,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Toàn bộ lượt thực hành AI đã HOÀN THÀNH của một Learner (mọi khóa học), read-only,
    /// mới nhất trước. Phục vụ khối "lịch sử AI scenario" + thống kê tóm tắt ở Learning Profile
    /// (UC-30, AC-5). Lọc theo learnerId qua Enrollment (BR-01).
    /// </summary>
    Task<List<AiScenarioHistoryItemDto>> GetCompletedScenarioHistoryByLearnerAsync(
        Guid learnerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Trang lịch sử thực hành AI đã HOÀN THÀNH của một Learner, mới nhất trước.
    /// Phục vụ màn "Xem tất cả lịch sử AI scenario" (UC-30). Lọc theo learnerId (BR-01).
    /// </summary>
    Task<(IEnumerable<AiScenarioHistoryItemDto> Items, int TotalCount)> GetPagedCompletedScenarioHistoryByLearnerAsync(
        Guid learnerId, int pageIndex, int pageSize, CancellationToken cancellationToken = default);
}
