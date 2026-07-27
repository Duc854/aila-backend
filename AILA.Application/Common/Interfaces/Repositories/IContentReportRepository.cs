using AILA.Domain.Entities;
using AILA.Domain.Enums;

namespace AILA.Application.Common.Interfaces.Repositories
{
    public interface IContentReportRepository : IGenericRepository<ContentReport>
    {
        /// <summary>
        /// Kiểm tra Learner đã có báo cáo đang chờ xử lý (Pending) cho đúng đối tượng này chưa
        /// (khóa học nếu materialId = null, hoặc học liệu cụ thể nếu materialId có giá trị).
        /// Dùng để chặn nộp trùng/double-submit (UC-33, edge case). Lọc theo learnerId (BR-02).
        /// </summary>
        Task<bool> HasPendingReportAsync(
            Guid learnerId,
            Guid? courseId,
            Guid? materialId,
            CancellationToken cancellationToken = default);
        /// <summary>
        /// UC-79
        /// Lấy danh sách báo cáo.
        /// </summary>
        Task<IEnumerable<ContentReport>> GetReportsAsync(
             ReportStatus? filterByStatus,
             bool? isCourseReport,  // ✅ true = Course, false = Material, null = All
             CancellationToken cancellationToken = default);

        /// <summary>
        /// UC-79
        /// Lấy chi tiết báo cáo.
        /// </summary>
        Task<ContentReport?> GetReportWithDetailsAsync(
            Guid reportId,
            CancellationToken cancellationToken = default);
    }
}

