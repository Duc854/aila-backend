using AILA.Domain.Entities;

namespace AILA.Application.Common.Interfaces.Repositories
{
    public interface ISubscriptionPlanRepository : IGenericRepository<SubscriptionPlan>
    {
        /// <summary>
        /// UC-09: Chỉ các gói Active, sắp xếp tăng dần theo DisplayOrder.
        /// Tie-break bằng TierLevel rồi CreatedAt để thứ tự ổn định giữa các lần tải.
        /// Lọc trạng thái thực hiện ở tầng dữ liệu, không lọc phía client.
        /// </summary>
        Task<IEnumerable<SubscriptionPlan>> GetActivePlansOrderedAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// UC-09 (edge case): Lấy gói đang bán theo Id để kiểm tra lại trạng thái tại thời
        /// điểm khởi tạo mua. Trả về null nếu gói không tồn tại.
        /// </summary>
        Task<SubscriptionPlan?> GetByIdReadOnlyAsync(
            Guid planId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// UC-90/UC-91/UC-92: Danh sách quản trị, gồm cả gói Inactive.
        /// </summary>
        Task<IEnumerable<SubscriptionPlan>> GetAllOrderedAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// UC-90/BR-01: Tên gói đã tồn tại (so sánh sau Trim, không phân biệt hoa thường).
        /// </summary>
        Task<bool> ExistsByNameAsync(
            string name,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// UC-90/BR-02: Cấp độ gói đã tồn tại.
        /// </summary>
        Task<bool> ExistsByTierLevelAsync(
            int tierLevel,
            CancellationToken cancellationToken = default);
    }
}
