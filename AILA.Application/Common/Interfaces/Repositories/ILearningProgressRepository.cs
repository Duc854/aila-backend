using AILA.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Common.Interfaces.Repositories
{
    public interface ILearningProgressRepository
        : IGenericRepository<LearningProgress>
    {
        Task<List<Guid>>
            GetCompletedMaterialIdsAsync(
                Guid courseId,
                Guid userId);

        Task<Guid?>
            GetCurrentMaterialIdAsync(
                Guid courseId,
                Guid userId);

        Task<LearningProgress?> GetByCompositeKeyAsync(Guid enrollmentId, Guid materialId, CancellationToken cancellationToken = default);
        Task AddAsync(LearningProgress progress, CancellationToken cancellationToken = default);
    }
}
