using AILA.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AILA.Application.Common.Interfaces.Repositories
{
    public interface IEnrollmentRepository : IGenericRepository<Enrollment>
    {
        /// Kiểm tra học viên đã tham gia khóa học này chưa
        Task<Enrollment?> GetByLearnerAndCourseAsync(Guid learnerId, Guid courseId);

        Task<Enrollment?> GetByCourseAndLearnerAsync(Guid courseId, Guid learnerId, CancellationToken cancellationToken = default);

        Task<bool> HasEnrollmentsForCourseAsync(Guid courseId, CancellationToken cancellationToken = default);

        Task<List<Enrollment>> GetEnrollmentsWithCourseByLearnerIdAsync(Guid learnerId, CancellationToken ct = default);

        /// <summary>
        /// Trang danh sách khóa học đã tham gia của một Learner (kèm Course + Category), sắp xếp
        /// truy cập gần nhất trước. Phục vụ màn "Xem tất cả khóa học" (UC-30). Lọc theo learnerId (BR-01).
        /// </summary>
        Task<(IEnumerable<Enrollment> Items, int TotalCount)> GetPagedEnrollmentsByLearnerAsync(
            Guid learnerId, int pageIndex, int pageSize, CancellationToken ct = default);

        /// <summary>
        /// Lấy danh sách các bản ghi Enrollment theo phạm vi khóa học và khoảng thời gian (UC-65)
        /// </summary>
        Task<List<Enrollment>> GetEnrollmentsInScopeAsync(
            List<Guid> courseIds, 
            DateTime fromDate, 
            DateTime toDate, 
            CancellationToken ct = default);

        void Update(Enrollment enrollment);

        Task<Enrollment?> GetWithCourseTagsAsync(
            Guid learnerId,
            Guid courseId,
            CancellationToken cancellationToken = default);

        Task<Enrollment?> GetWithCourseTagsByIdAsync(
            Guid enrollmentId,
            CancellationToken cancellationToken = default);
    }
}
