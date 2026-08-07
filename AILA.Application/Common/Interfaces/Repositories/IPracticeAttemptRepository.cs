// Application/Common/Interfaces/Repositories/IPracticeAttemptRepository.cs
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
}
