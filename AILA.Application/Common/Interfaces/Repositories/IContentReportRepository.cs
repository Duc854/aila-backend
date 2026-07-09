using AILA.Domain.Entities;

namespace AILA.Application.Common.Interfaces.Repositories
{
    public interface IContentReportRepository : IGenericRepository<ContentReport>
    {
        /// <summary>
        /// Kiểm tra Learner đã có báo cáo đang chờ xử lý (Pending) cho đúng khóa học này chưa.
        /// Dùng để chặn nộp trùng/double-submit (UC-33, edge case). Lọc theo learnerId (BR-02).
        /// </summary>
        Task<bool> HasPendingCourseReportAsync(Guid learnerId, Guid courseId, CancellationToken cancellationToken = default);
    }
}
