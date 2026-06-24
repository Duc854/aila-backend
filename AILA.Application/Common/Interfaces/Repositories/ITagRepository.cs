using AILA.Domain.Entities;

namespace AILA.Application.Common.Interfaces.Repositories
{
    public interface ITagRepository : IGenericRepository<Tag>
    {
        /// Lấy tất cả Tag đã được duyệt (IsPublished = true), sắp xếp theo tên
        Task<List<Tag>> GetPublishedTagsAsync();
    }
}
