using AILA.Application.Common.Interfaces.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace AILA.Application.Common.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        ICourseRepository           Courses            { get; }
        ILearningProgressRepository LearningProgresses { get; }
        ITagRepository              Tags               { get; }
        ICategoryRepository         Categories         { get; }
        IEnrollmentRepository       Enrollments        { get; }
        IUserRepository             Users              { get; }
        INotificationRepository     Notifications      { get; }
        IMaterialRepository         Materials          { get; }
        IBlogPostRepository         BlogPosts          { get; }
        ILearnerRepository          Learners           { get; }
        IExpertRepository           Experts            { get; }
        IModuleRepository Modules { get; }
        IQuizRepository Quizzes { get; }
        IContentReportRepository ContentReports { get; }
        IQuestionRepository Questions { get; }
        IAnswerOptionRepository AnswerOptions { get; }
        IGenericRepository<T> Repository<T>() where T : class;

        /// <summary>
        /// Lưu toàn bộ thay đổi dữ liệu của phiên làm việc vào Database dưới dạng một Transaction
        /// </summary>
        /// <returns>Số lượng bản ghi bị ảnh hưởng</returns>
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Bắt đầu một Transaction thủ công (Dùng khi cần xử lý các tác vụ phức tạp liên quan đến bên thứ 3)
        /// </summary>
        Task BeginTransactionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Xác nhận và chốt dữ liệu khi Transaction thành công
        /// </summary>
        Task CommitTransactionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Hoàn tác dữ liệu về trạng thái cũ nếu xảy ra lỗi trong Transaction
        /// </summary>
        Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
    }
}
