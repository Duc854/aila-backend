using AILA.Application.Common.Interfaces.Repositories;
using AILA.Domain.Entities;

namespace AILA.Infrastructure.Persistence.Repositories
{
    public class LearnerRepository : GenericRepository<Learner>, ILearnerRepository
    {
        public LearnerRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
}
