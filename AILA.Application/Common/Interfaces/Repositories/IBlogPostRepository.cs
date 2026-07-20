using AILA.Domain.Entities;
using System.Reflection.Metadata;

namespace AILA.Application.Common.Interfaces.Repositories
{
    public interface IBlogPostRepository : IGenericRepository<BlogPost>
    {
        Task<BlogPost?> GetBlogDetailAsync(Guid id, CancellationToken ct = default);
        Task<List<BlogPost>> GetRelatedBlogsAsync(Guid currentBlogId, int count, CancellationToken ct = default);
        Task IncrementViewCountAsync(Guid id, CancellationToken ct = default);
        Task<(IEnumerable<BlogPost> Items, int TotalCount)> GetPagedBlogsAsync(string? search,int pageNumber,int pageSize,CancellationToken cancellationToken);

        // Admin
        Task<(IEnumerable<BlogPost> Items, int TotalCount)> GetPagedAdminBlogsAsync(
            string? search,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken);

        Task<bool> ExistsSlugAsync(
            string slug,
            Guid? excludeId = null,
            CancellationToken ct = default);
    }
}
