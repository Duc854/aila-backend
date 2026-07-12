using AILA.Domain.Entities;
using AILA.Domain.Enums;

namespace AILA.Application.Common.Interfaces.Repositories
{
    public interface IContentReportRepository : IGenericRepository<ContentReport>
    {
        /// <summary>
        /// Kiểm tra Learner đã có báo cáo đang chờ xử lý (Pending) cho đúng khóa học này chưa.
        /// Dùng để chặn nộp trùng/double-submit (UC-33, edge case). Lọc theo learnerId (BR-02).
        /// </summary>
        Task<bool> HasPendingCourseReportAsync(Guid learnerId, Guid courseId, CancellationToken cancellationToken = default);
        /// <summary>
        /// UC-79
        /// Lấy danh sách báo cáo.
        /// </summary>
        Task<IEnumerable<ContentReport>> GetReportsAsync(
            ReportStatus? status,
            bool? isCourseReport,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// UC-79
        /// Lấy chi tiết báo cáo.
        /// </summary>
        Task<ContentReport?> GetReportByIdAsync(
            Guid reportId,
            CancellationToken cancellationToken = default);
    }
}

