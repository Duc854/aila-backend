using AILA.Application.Common.Interfaces.Repositories;
using AILA.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AILA.Infrastructure.Persistence.Repositories
{
    public class BlogPostRepository : GenericRepository<BlogPost>, IBlogPostRepository
    {
        public BlogPostRepository(ApplicationDbContext context) : base(context) { }

        public async Task<BlogPost?> GetBlogDetailAsync(Guid id, CancellationToken ct = default)
        {
            return await _context.BlogPosts
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == id, ct);
        }

        public async Task<List<BlogPost>> GetRelatedBlogsAsync(
            Guid currentBlogId, int count, CancellationToken ct = default)
        {
            return await _context.BlogPosts
                .Where(b => b.Id != currentBlogId && b.IsPublished)
                .OrderByDescending(b => b.PublishedAt)
                .Take(count)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        // Dùng ExecuteUpdateAsync để tăng ViewCount nguyên tử, tránh race condition khi nhiều user truy cập cùng lúc
        public async Task IncrementViewCountAsync(Guid id, CancellationToken ct = default)
        {
            await _context.BlogPosts
                .Where(b => b.Id == id)
                .ExecuteUpdateAsync(
                    s => s.SetProperty(b => b.ViewCount, b => b.ViewCount + 1),
                    ct);
        }
    }
}
