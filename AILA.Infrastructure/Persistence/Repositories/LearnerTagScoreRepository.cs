using AILA.Application.Common.Interfaces.Repositories;
using AILA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Infrastructure.Persistence.Repositories
{
    public class LearnerTagScoreRepository
        : GenericRepository<LearnerTagScore>, ILearnerTagScoreRepository
    {
        public LearnerTagScoreRepository(
            ApplicationDbContext context)
            : base(context)
        {
        }


        public async Task<LearnerTagScore?> GetByLearnerAndTagAsync(
            Guid learnerId,
            Guid tagId,
            CancellationToken cancellationToken = default)
        {
            return await _context.LearnerTagScores
                .FirstOrDefaultAsync(
                    x => x.LearnerId == learnerId
                      && x.TagId == tagId,
                    cancellationToken);
        }

        public async Task<List<LearnerTagScore>> GetByLearnerIdAsync(
            Guid learnerId,
            CancellationToken cancellationToken = default)
        {
            return await _context.LearnerTagScores
                .Where(x => x.LearnerId == learnerId)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<LearnerTagScore>>
            GetByLearnerIdAndTagIdsAsync(
                Guid learnerId,
                IEnumerable<Guid> tagIds,
                CancellationToken cancellationToken = default)
        {
            var ids = tagIds.ToList();

            return await _context.LearnerTagScores
                .Where(x =>
                    x.LearnerId == learnerId &&
                    ids.Contains(x.TagId))
                .ToListAsync(cancellationToken);
        }
    }
}
