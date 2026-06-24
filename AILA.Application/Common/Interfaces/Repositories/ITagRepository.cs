using AILA.Domain.Entities;

namespace AILA.Application.Common.Interfaces.Repositories
{
    public interface ITagRepository : IGenericRepository<Tag>
    {
        /// <summary>Lấy tất cả Tag đã được duyệt (IsPublished = true), sắp xếp theo tên</summary>
        Task<List<Tag>> GetPublishedTagsAsync();

        Task<List<Tag>> GetByIdsAsync(IEnumerable<Guid> tagIds, CancellationToken ct = default);
    }
}
