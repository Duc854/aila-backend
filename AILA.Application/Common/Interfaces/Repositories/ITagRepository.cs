using AILA.Domain.Entities;

namespace AILA.Application.Common.Interfaces.Repositories
{
    public interface ITagRepository : IGenericRepository<Tag>
    {
        /// <summary>Lấy tất cả Tag đã được duyệt (IsPublished = true), sắp xếp theo tên</summary>
        Task<List<Tag>> GetPublishedTagsAsync();

        Task<List<Tag>> GetByIdsAsync(IEnumerable<Guid> tagIds, CancellationToken ct = default);

        /// Lấy tag kèm PublishRequest để kiểm tra trước khi tạo yêu cầu xét duyệt.
        Task<Tag?> GetWithPublishRequestAsync(Guid tagId, CancellationToken ct = default);

      
        /// Kiểm tra code tag đã tồn tại chưa (tránh trùng slug).
       
        Task<bool> CodeExistsAsync(string code, CancellationToken ct = default);

       
        /// Lấy danh sách tag do Expert tạo, kèm trạng thái PublishRequest.
       
        Task<List<Tag>> GetByExpertAsync(Guid expertId, CancellationToken ct = default);
    }
}
