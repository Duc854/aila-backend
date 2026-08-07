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

        /// Lấy thông tin tag theo code slug. Trả về null nếu không tìm thấy.
        Task<Tag?> GetByCodeAsync(string code, CancellationToken ct = default);

       
        /// Lấy danh sách tag do Expert tạo, kèm trạng thái PublishRequest.
       
        Task<List<Tag>> GetByExpertAsync(Guid expertId, CancellationToken ct = default);

        /// UC-85: Danh sách request chờ duyệt
        Task<List<Tag>> GetPendingVerificationRequestsAsync(
            CancellationToken ct = default);

        /// UC-85: Chi tiết request
        Task<Tag?> GetVerificationRequestByIdAsync(
            Guid tagId,
            CancellationToken ct = default);

        /// UC-86~88: Danh sách System Tag
        Task<List<Tag>> GetSystemTagsAsync(
            CancellationToken ct = default);

        /// UC-87~88: Kiểm tra tag đã được gán vào Course chưa
        Task<bool> IsAssignedToCourseAsync(
            Guid tagId,
            CancellationToken ct = default);
        Task<int> GetUsageCountAsync(
    Guid tagId,
    CancellationToken ct = default);

        Task<List<Tag>> GetPublishedByIdsAsync(
        IEnumerable<Guid> ids,
        CancellationToken cancellationToken);

        Task<List<Tag>> GetByCodesAsync(
    List<string> codes,
    CancellationToken cancellationToken = default);
    }
}
