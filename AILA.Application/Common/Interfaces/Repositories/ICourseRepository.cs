using AILA.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Common.Interfaces.Repositories
{
    public interface ICourseRepository : IGenericRepository<Course>
    {
        // HÀM GIẢI QUYẾT BÀI TOÁN LẤY CẤU TRÚC PHÂN CẤP CỦA BẠN:
        // Lấy chi tiết Khóa học gồm toàn bộ Modules, Materials, Video và Document đi kèm.
        Task<Course?> GetCourseWithFullContentAsync(Guid courseId);

        /// Tìm kiếm và lọc danh sách khóa học đã công khai, kèm thông tin Author, Category, Tags.
        /// Trả về dạng phân trang.
        Task<(List<Course> Items, int TotalCount)> SearchCoursesAsync(
            string? keyword,
            Guid? categoryId,
            Guid? tagId,
            string? level,
            int pageIndex,
            int pageSize);

        /// Lấy chi tiết khóa học kèm đầy đủ thông tin liên quan:
        /// Category, Expert.User, Tags, Modules.Materials
        Task<Course?> GetCourseDetailAsync(Guid courseId);

        /// Đếm số lượng Materials (bài học) của một khóa học
        Task<int> CountMaterialsAsync(Guid courseId);

        /// Lấy danh sách khóa học đã published của một Expert, sắp xếp mới nhất trước.
        /// Dùng cho Public Expert Profile (read-only).
        Task<List<Course>> GetPublishedByExpertAsync(Guid expertId, CancellationToken ct = default);

       
        /// Lấy toàn bộ khóa học (cả draft lẫn published) của một Expert,
        /// kèm Category và Tags. Dùng cho trang quản lý của Expert.
        Task<(List<Course> Items, int TotalCount)> GetByExpertAsync(
            Guid expertId,
            string? keyword,
            bool? isPublished,
            int pageIndex,
            int pageSize,
            CancellationToken ct = default);

       
        /// Lấy khóa học kèm Tags để phục vụ thao tác ghi (Edit/Publish/Unpublish).
        /// Dùng AsTracking để EF Core có thể lưu thay đổi.     
        Task<Course?> GetWithTagsForUpdateAsync(Guid courseId, CancellationToken ct = default);
    }
}
