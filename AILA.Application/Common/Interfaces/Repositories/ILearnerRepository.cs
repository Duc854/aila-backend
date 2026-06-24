using AILA.Domain.Entities;
using AILA.Domain.Enums;

namespace AILA.Application.Common.Interfaces.Repositories
{
    public interface ILearnerRepository : IGenericRepository<Learner>
    {
        Task<Learner?> GetWithUserAndGoalsAsync(Guid userId, CancellationToken ct = default);
        Task<Learner?> GetReadonlyWithUserAndGoalsAsync(Guid userId, CancellationToken ct = default);
        void SetLearnerDetails(Learner learner, LearnerType? learnerType, KnowledgeLevel? knowledgeLevel);
    }
}
