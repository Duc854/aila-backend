using AILA.Application.Common.Interfaces.Repositories;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AILA.Infrastructure.Persistence.Repositories
{
    public class LearnerRepository(ApplicationDbContext context)
        : GenericRepository<Learner>(context), ILearnerRepository
    {
        public async Task<Learner?> GetWithUserAndGoalsAsync(Guid userId, CancellationToken ct = default)
            => await _context.Learners
                .Include(l => l.User)
                .Include(l => l.LearningGoals)
                .FirstOrDefaultAsync(l => l.UserId == userId, ct);

        public async Task<Learner?> GetReadonlyWithUserAndGoalsAsync(Guid userId, CancellationToken ct = default)
            => await _context.Learners
                .AsNoTracking()
                .Include(l => l.User)
                .Include(l => l.LearningGoals)
                .FirstOrDefaultAsync(l => l.UserId == userId, ct);

        public void SetLearnerDetails(Learner learner, LearnerType? learnerType, KnowledgeLevel? knowledgeLevel)
        {
            if (learnerType.HasValue)
                _context.Entry(learner).Property("LearnerType").CurrentValue = learnerType;
            if (knowledgeLevel.HasValue)
                _context.Entry(learner).Property("KnowledgeLevel").CurrentValue = knowledgeLevel;
        }
    }
}
