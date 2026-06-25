using AILA.Application.Common.Interfaces.Repositories;
using AILA.Domain.Entities;

namespace AILA.Infrastructure.Persistence.Repositories
{
    public class TagRepository : GenericRepository<Tag>, ITagRepository
    {
        public TagRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
}
