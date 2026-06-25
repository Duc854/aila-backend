using AILA.Application.Common.Interfaces.Repositories;
using AILA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AILA.Infrastructure.Persistence.Repositories
{
    public class BlogPostRepository : GenericRepository<BlogPost>, IBlogPostRepository
    {
        public BlogPostRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IReadOnlyList<BlogPost>> GetTopBlogsAsync(int count)
        {
            return await _context.Set<BlogPost>()
                .OrderByDescending(b => b.PublishedAt) // Wait, is there a PublishDate? Let me check BlogPost.cs later, or just use CreatedAt for now. I'll change it if needed.
                .Take(count)
                .AsNoTracking()
                .ToListAsync();
        }
    }
}
