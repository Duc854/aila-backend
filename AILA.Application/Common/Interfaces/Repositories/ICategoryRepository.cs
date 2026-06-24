using AILA.Domain.Entities;

namespace AILA.Application.Common.Interfaces.Repositories
{
    public interface ICategoryRepository : IGenericRepository<Category>
    {
        /// Lấy tất cả danh mục đang hoạt động (IsActive = true), sắp xếp theo OrderIndex
        Task<List<Category>> GetActiveCategoriesAsync();
    }
}
