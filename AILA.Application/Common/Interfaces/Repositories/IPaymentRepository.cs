using AILA.Domain.Entities;
using AILA.Domain.Enums;

namespace AILA.Application.Common.Interfaces.Repositories
{
    public interface IPaymentRepository : IGenericRepository<Payment>
    {
        /// <summary>
        /// UC-19: Lấy payment Pending còn hiệu lực của learner (dùng để huỷ trước khi tạo mới).
        /// </summary>
        Task<Payment?> GetPendingPaymentByLearnerIdAsync(
            Guid learnerId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// UC-19 Webhook: Tìm payment theo OrderCode để xác nhận thanh toán từ SePay.
        /// </summary>
        Task<Payment?> GetByOrderCodeAsync(
            string orderCode,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// UC-20: Lấy lịch sử thanh toán của learner có hỗ trợ lọc theo ngày và phân trang.
        /// </summary>
        Task<(IEnumerable<Payment> Items, int TotalCount)> GetPaymentHistoryAsync(
            Guid learnerId,
            DateTime? fromDate,
            DateTime? toDate,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// UC-20: Lấy chi tiết một payment, đảm bảo chỉ learner sở hữu mới xem được (BR-01).
        /// </summary>
        Task<Payment?> GetPaymentDetailAsync(
            Guid paymentId,
            Guid learnerId,
            CancellationToken cancellationToken = default);
    }
}
