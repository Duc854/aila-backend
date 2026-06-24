using AILA.Domain.Entities;

namespace AILA.Application.Common.Interfaces.Repositories
{
    public interface IUserRepository : IGenericRepository<User>
    {
        /// <summary>Tìm User theo email (không phân biệt hoa thường)</summary>
        Task<User?> GetByEmailAsync(string email);

        /// <summary>
        /// Lấy thông tin Expert kèm profile (User + Expert),
        /// dùng cho Expert Profile và Expert Login.
        /// </summary>
        Task<User?> GetExpertWithProfileAsync(Guid userId);
    }
}
