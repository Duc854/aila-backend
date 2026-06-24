using AILA.Application.Common.Interfaces.Repositories;

namespace AILA.Application.Common.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        ICourseRepository           Courses               { get; }
        ILearningProgressRepository LearningProgresses    { get; }
        IUserRepository             Users                 { get; }
        INotificationRepository     Notifications         { get; }

        IGenericRepository<T> Repository<T>() where T : class;

        /// <summary>Lưu toàn bộ thay đổi vào Database trong một Transaction</summary>
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

        /// <summary>Bắt đầu Transaction thủ công</summary>
        Task BeginTransactionAsync(CancellationToken cancellationToken = default);

        /// <summary>Xác nhận và chốt dữ liệu khi Transaction thành công</summary>
        Task CommitTransactionAsync(CancellationToken cancellationToken = default);

        /// <summary>Hoàn tác dữ liệu về trạng thái cũ nếu xảy ra lỗi</summary>
        Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
    }
}
