using AILA.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AILA.Application.Common.Interfaces.Repositories
{
    public interface IBlogPostRepository : IGenericRepository<BlogPost>
    {
        Task<IReadOnlyList<BlogPost>> GetTopBlogsAsync(int count);
    }
}
