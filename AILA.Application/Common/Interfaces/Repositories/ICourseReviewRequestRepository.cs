using AILA.Domain.Entities;
using AILA.Domain.Enums;

namespace AILA.Application.Common.Interfaces.Repositories;

/// <summary>
/// Repository cho CourseReviewRequest — yêu cầu mở lại khóa học bị khoá.
/// </summary>
public interface ICourseReviewRequestRepository : IGenericRepository<CourseReviewRequest>
{
    /// <summary>
    /// Kiểm tra khóa học có đang có request Pending không (chặn gửi trùng).
    /// </summary>
    Task<bool> HasPendingRequestAsync(Guid courseId, CancellationToken ct = default);

    /// <summary>
    /// Lấy request kèm Course (tracked) để domain method có thể ghi thay đổi.
    /// </summary>
    Task<CourseReviewRequest?> GetWithCourseAsync(Guid requestId, CancellationToken ct = default);

    /// <summary>
    /// Lấy danh sách requests cho Admin, có thể lọc theo status.
    /// Kèm Course và Expert.User để hiển thị thông tin đầy đủ.
    /// </summary>
    Task<List<CourseReviewRequest>> GetAllWithDetailsAsync(
        CourseReviewRequestStatus? status,
        CancellationToken ct = default);

    /// <summary>
    /// Lấy danh sách requests của một Expert cụ thể.
    /// </summary>
    Task<List<CourseReviewRequest>> GetByExpertAsync(Guid expertId, CancellationToken ct = default);
}
