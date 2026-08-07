using AILA.Domain.Entities;
using AILA.Domain.Enums;

namespace AILA.Application.Common.Interfaces.Repositories
{
    public interface IExpertEvaluationRequestRepository
        : IGenericRepository<ExpertEvaluationRequest>
    {
        /// <summary>
        /// Lấy yêu cầu kèm kết quả chuyên gia đã chấm (nếu có).
        /// </summary>
        Task<ExpertEvaluationRequest?> GetByIdWithEvaluationAsync(
            Guid requestId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// BR-02 (UC-29): mỗi lượt thực hành chỉ được yêu cầu đánh giá một lần.
        /// Yêu cầu đã hủy không tính, học viên được gửi lại.
        /// </summary>
        Task<bool> HasActiveRequestForAttemptAsync(
            Guid practiceAttemptId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// UC-63: hàng chờ của chuyên gia, phân trang, mặc định đang xử lý trước / cũ nhất trước.
        /// </summary>
        Task<(IReadOnlyList<ExpertEvaluationRequest> Items, int TotalCount)> GetAssignedPageAsync(
            Guid expertId,
            ExpertEvaluationRequestStatus? status,
            int pageIndex,
            int pageSize,
            CancellationToken cancellationToken = default);
    }
}
