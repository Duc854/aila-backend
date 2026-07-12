using AILA.Application.Common.Interfaces.Repositories;
using AILA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Metadata;

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

        public async Task<(IEnumerable<BlogPost> Items, int TotalCount)> GetPagedBlogsAsync(
            string? search,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken)
        {
            // Bắt đầu bằng việc tối ưu hóa hiệu năng, tắt tracking vì đây là luồng đọc dữ liệu (Query)
            var query = _context.BlogPosts.AsNoTracking().Where(b => b.IsPublished);

            // Áp dụng Filter tìm kiếm (Nếu phía client truyền lên)
            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowerSearch = search.ToLower();
                query = query.Where(b => b.Title.ToLower().Contains(lowerSearch)
                                      || b.Content.ToLower().Contains(lowerSearch));
            }

            // Đếm tổng số lượng bản ghi thỏa mãn điều kiện filter trước khi phân trang
            int totalCount = await query.CountAsync(cancellationToken);

            // Áp dụng phân trang (Skip, Take) và sắp xếp bài mới nhất lên đầu
            var items = await query
                .OrderByDescending(b => b.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }
    }
}
