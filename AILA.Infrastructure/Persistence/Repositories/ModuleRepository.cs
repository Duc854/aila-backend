using AILA.Application.Common.Interfaces.Repositories;
using AILA.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AILA.Infrastructure.Persistence.Repositories
{
    public class ModuleRepository : GenericRepository<Module>, IModuleRepository
    {
        public ModuleRepository(ApplicationDbContext context) : base(context) { }

        /// <summary>
        /// Lấy tất cả Module của Course, kèm Materials để tính MaterialCount.
        /// Sắp xếp theo OrderIndex tăng dần.
        /// AsNoTracking vì đây là query đọc thuần túy.
        /// </summary>
        public async Task<List<Module>> GetByCourseIdAsync(
            Guid courseId,
            CancellationToken ct = default)
        {
            return await _context.Modules
                .Include(m => m.Materials)
                .Where(m => m.CourseId == courseId)
                .OrderBy(m => m.OrderIndex)
                .ToListAsync(ct);
        }

        /// <summary>
        /// Lấy Module kèm Course cha để xác minh ExpertId (quyền sở hữu).
        /// Dùng Tracking (không AsNoTracking) để EF có thể theo dõi thay đổi khi update/delete.
        /// </summary>
        public async Task<Module?> GetWithCourseAsync(
            Guid moduleId,
            CancellationToken ct = default)
        {
            return await _context.Modules
                .Include(m => m.Course)
                .Include(m => m.Materials)
                .FirstOrDefaultAsync(m => m.Id == moduleId, ct);
        }
    }
}
