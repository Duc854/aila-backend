using AILA.Application.Common.Interfaces.Repositories;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Infrastructure.Persistence.Repositories
{
    public class CourseRepository : GenericRepository<Course>, ICourseRepository
    {
        public CourseRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<List<Course>> GetTopCoursesWithAuthorAsync(int count)
        {
            return await _context.Courses
                .Include(c => c.Expert)
                .Where(c => c.IsPublished)
                .Take(count)
                .ToListAsync();
        }

        // Thực hiện Eager Loading bóc tách dữ liệu lồng nhau tránh lỗi N+1 Query
        public async Task<Course?> GetCourseWithFullContentAsync(Guid courseId)
        {
            return await _context.Courses
                .Include(c => c.Modules)
                    .ThenInclude(m => m.Materials)
                        .ThenInclude(mat => mat.VideoDetails)
                .Include(c => c.Modules)
                    .ThenInclude(m => m.Materials)
                        .ThenInclude(mat => mat.DocumentDetails)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == courseId);
        }

        
        /// Tìm kiếm và lọc khóa học đã publish, kèm thông tin Author, Category, Tags.
        /// Hỗ trợ phân trang.
        
        public async Task<(List<Course> Items, int TotalCount)> SearchCoursesAsync(
            string? keyword,
            Guid? categoryId,
            Guid? tagId,
            string? level,
            int pageIndex,
            int pageSize)
        {
            var query = _context.Courses
                .Include(c => c.Category)
                .Include(c => c.Expert)
                    .ThenInclude(e => e.User)
                .Include(c => c.CourseTags)
                .Where(c => c.IsPublished)
                .AsQueryable();

            // Lọc theo từ khóa tìm kiếm (tên hoặc mô tả)
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var lowerKeyword = keyword.ToLower().Trim();
                query = query.Where(c =>
                    c.Name.ToLower().Contains(lowerKeyword) ||
                    (c.Description != null && c.Description.ToLower().Contains(lowerKeyword)));
            }

            // Lọc theo danh mục
            if (categoryId.HasValue)
                query = query.Where(c => c.CategoryId == categoryId.Value);

            // Lọc theo tag
            if (tagId.HasValue)
                query = query.Where(c => c.CourseTags.Any(t => t.Id == tagId.Value));

            // Lọc theo level (convert string sang enum)
            if (!string.IsNullOrWhiteSpace(level) && Enum.TryParse<KnowledgeLevel>(level, true, out var parsedLevel))
                query = query.Where(c => c.Level == parsedLevel);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(c => c.CreatedAt)
                .Skip(pageIndex * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();

            return (items, totalCount);
        }

        
        /// Lấy chi tiết khóa học với đầy đủ thông tin liên quan sâu:
        /// Category, Expert.User, Tags, Modules (theo OrderIndex), Materials (theo OrderIndex)
       
        public async Task<Course?> GetCourseDetailAsync(Guid courseId)
        {
            return await _context.Courses
                .Include(c => c.Category)
                .Include(c => c.Expert)
                    .ThenInclude(e => e.User)
                .Include(c => c.CourseTags)
                .Include(c => c.Modules)
                    .ThenInclude(m => m.Materials)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == courseId);
        }

       
        /// Đếm tổng số Materials (bài học) trong tất cả Modules của một khóa học
        public async Task<int> CountMaterialsAsync(Guid courseId)
        {
            return await _context.Materials
                .Where(mat => mat.Module.CourseId == courseId)
                .CountAsync();
        }
    }
}
