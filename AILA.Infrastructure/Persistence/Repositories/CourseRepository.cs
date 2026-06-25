using AILA.Application.Common.Interfaces.Repositories;
using AILA.Domain.Entities;
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

        public async Task<IReadOnlyList<Course>> GetTopCoursesAsync(int count)
        {
            return await _context.Courses
                .Where(c => c.IsPublished)
                .OrderByDescending(c => c.CreatedAt) // Assuming CreatedAt since Rating doesn't exist
                .Take(count)
                .AsNoTracking()
                .ToListAsync();
        }
    }
}
