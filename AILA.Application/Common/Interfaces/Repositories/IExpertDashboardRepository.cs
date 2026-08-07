using AILA.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AILA.Application.Common.Interfaces.Repositories
{
    public interface IExpertDashboardRepository
    {
        /// <summary>
        /// UC-65 / BR-01: Lấy danh sách các khóa học đã xuất bản của Expert
        /// </summary>
        Task<List<Course>> GetPublishedCoursesByExpertAsync(
            Guid expertId, 
            CancellationToken ct = default);

        /// <summary>
        /// UC-65 / BR-04: Lấy danh sách các bản ghi Enrollment theo phạm vi khóa học và khoảng thời gian
        /// </summary>
        Task<List<Enrollment>> GetEnrollmentsInScopeAsync(
            List<Guid> courseIds, 
            DateTime fromDate, 
            DateTime toDate, 
            CancellationToken ct = default);

        /// <summary>
        /// UC-65 / BR-04: Đếm tổng số lượt làm bài Quiz trong phạm vi
        /// </summary>
        Task<int> GetQuizAttemptsCountInScopeAsync(
            List<Guid> courseIds, 
            DateTime fromDate, 
            DateTime toDate, 
            CancellationToken ct = default);

        /// <summary>
        /// UC-65 / BR-04: Đếm tổng số lượt thực hành Kịch bản AI trong phạm vi
        /// </summary>
        Task<int> GetPracticeAttemptsCountInScopeAsync(
            List<Guid> courseIds, 
            DateTime fromDate, 
            DateTime toDate, 
            CancellationToken ct = default);
    }
}
