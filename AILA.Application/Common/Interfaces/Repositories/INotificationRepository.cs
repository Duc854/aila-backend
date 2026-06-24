using AILA.Domain.Entities;

namespace AILA.Application.Common.Interfaces.Repositories
{
    public interface INotificationRepository : IGenericRepository<Notification>
    {
        /// <summary>Lấy toàn bộ thông báo của một User, mới nhất trước</summary>
        Task<List<Notification>> GetAllByUserIdAsync(Guid userId);
    }
}
