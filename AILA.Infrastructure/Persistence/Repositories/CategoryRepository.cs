using AILA.Application.Common.Interfaces.Repositories;
using AILA.Domain.Entities;

namespace AILA.Infrastructure.Persistence.Repositories
{
    public class CategoryRepository : GenericRepository<Category>, ICategoryRepository
    {
        public CategoryRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
}
