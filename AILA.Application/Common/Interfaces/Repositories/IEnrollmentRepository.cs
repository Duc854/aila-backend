using AILA.Domain.Entities;

namespace AILA.Application.Common.Interfaces.Repositories
{
    public interface IEnrollmentRepository : IGenericRepository<Enrollment>
    {
        /// Kiểm tra học viên đã tham gia khóa học này chưa
        Task<Enrollment?> GetByLearnerAndCourseAsync(Guid learnerId, Guid courseId);

        Task<Enrollment?> GetByCourseAndLearnerAsync(Guid courseId, Guid learnerId, CancellationToken cancellationToken = default);

        Task<List<Enrollment>> GetEnrollmentsWithCourseByLearnerIdAsync(Guid learnerId, CancellationToken ct = default);

        /// <summary>
        /// Trang danh sách khóa học đã tham gia của một Learner (kèm Course + Category), sắp xếp
        /// truy cập gần nhất trước. Phục vụ màn "Xem tất cả khóa học" (UC-30). Lọc theo learnerId (BR-01).
        /// </summary>
        Task<(IEnumerable<Enrollment> Items, int TotalCount)> GetPagedEnrollmentsByLearnerAsync(
            Guid learnerId, int pageIndex, int pageSize, CancellationToken ct = default);

        void Update(Enrollment enrollment);
    }
}
