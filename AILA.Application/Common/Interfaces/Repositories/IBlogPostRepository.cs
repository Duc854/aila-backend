using AILA.Domain.Entities;

namespace AILA.Application.Common.Interfaces.Repositories
{
    public interface IBlogPostRepository : IGenericRepository<BlogPost>
    {
        Task<BlogPost?> GetBlogDetailAsync(Guid id, CancellationToken ct = default);
        Task<List<BlogPost>> GetRelatedBlogsAsync(Guid currentBlogId, int count, CancellationToken ct = default);
        Task IncrementViewCountAsync(Guid id, CancellationToken ct = default);
    }
}
