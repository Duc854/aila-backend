using AILA.Domain.Entities;

namespace AILA.Application.Common.Interfaces.Repositories
{
    public interface ICategoryRepository : IGenericRepository<Category>
    {
        /// Lấy tất cả danh mục đang hoạt động (IsActive = true), sắp xếp theo OrderIndex
        Task<List<Category>> GetActiveCategoriesAsync();
    

        /// <summary>
        /// UC-80: Danh sách category theo OrderIndex.
        /// </summary>
        Task<IEnumerable<Category>> GetAllOrderedAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// UC-81: Kiểm tra tên category đã tồn tại.
        /// </summary>
        Task<bool> ExistsByNameAsync(
            string name,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// UC-82: Kiểm tra tên bị trùng (ngoại trừ chính nó).
        /// </summary>
        Task<bool> ExistsByNameExceptIdAsync(
            Guid categoryId,
            string name,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// UC-83: Category đang được course sử dụng.
        /// </summary>
        Task<bool> HasCoursesAsync(
            Guid categoryId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// UC-84: Lấy toàn bộ category theo danh sách id.
        /// </summary>
        Task<List<Category>> GetByIdsAsync(
            IEnumerable<Guid> ids,
            CancellationToken cancellationToken = default);
    }
}
