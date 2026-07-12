using AILA.Domain.Entities;

namespace AILA.Application.Common.Interfaces.Repositories
{
    public interface IModuleRepository : IGenericRepository<Module>
    {
        /// <summary>
        /// Lấy tất cả Module của một Course, sắp xếp theo OrderIndex.
        /// Kèm số lượng Material (MaterialCount) để hiển thị trên danh sách.
        /// </summary>
        Task<List<Module>> GetByCourseIdAsync(Guid courseId, CancellationToken ct = default);

        /// <summary>
        /// Lấy một Module kèm thông tin Course cha để xác minh quyền sở hữu của Expert.
        /// </summary>
        Task<Module?> GetWithCourseAsync(Guid moduleId, CancellationToken ct = default);
    }
}
