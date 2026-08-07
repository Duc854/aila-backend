using AILA.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Common.Interfaces.Repositories
{
    public interface ILearnerTagScoreRepository : IGenericRepository<LearnerTagScore>
    {
        Task<LearnerTagScore?> GetByLearnerAndTagAsync(
            Guid learnerId,
            Guid tagId,
            CancellationToken cancellationToken = default);
        Task<List<LearnerTagScore>> GetByLearnerIdAsync(
            Guid learnerId,
            CancellationToken cancellationToken = default);
        Task<List<LearnerTagScore>> GetByLearnerIdAndTagIdsAsync(
            Guid learnerId,
            IEnumerable<Guid> tagIds,
            CancellationToken cancellationToken = default);
        Task<List<LearnerTagScore>> GetForRecommendationAsync(
            Guid learnerId,
            int minimumScore,
            CancellationToken cancellationToken = default);
    }
}
