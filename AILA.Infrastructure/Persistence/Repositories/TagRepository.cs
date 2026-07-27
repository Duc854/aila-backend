using AILA.Application.Common.Interfaces.Repositories;
using AILA.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AILA.Infrastructure.Persistence.Repositories
{
    // Dùng primary constructor theo style của người khác
    public class TagRepository(ApplicationDbContext context)
        : GenericRepository<Tag>(context), ITagRepository
    {
        public async Task<List<Tag>> GetPublishedTagsAsync()
            => await _context.Tags
                .Where(t => t.IsPublished)
                .OrderBy(t => t.Name)
                .AsNoTracking()
                .ToListAsync();

        public async Task<List<Tag>> GetByIdsAsync(IEnumerable<Guid> tagIds, CancellationToken ct = default)
            => await _context.Tags
                .Where(t => tagIds.Contains(t.Id))
                .ToListAsync(ct);

        
       
        /// Lấy Tag kèm PublishRequest — dùng AsTracking để domain method có thể ghi lại thay đổi.
        public async Task<Tag?> GetWithPublishRequestAsync(Guid tagId, CancellationToken ct = default)
            => await _context.Tags
                .Include(t => t.PublishRequest)
                .FirstOrDefaultAsync(t => t.Id == tagId, ct);

       
        /// Kiểm tra code slug đã tồn tại trong hệ thống chưa.
        public async Task<bool> CodeExistsAsync(string code, CancellationToken ct = default)
            => await _context.Tags
                .AnyAsync(t => t.Code == code.ToLower().Trim().Replace(" ", "-"), ct);

        public async Task<Tag?> GetByCodeAsync(string code, CancellationToken ct = default)
            => await _context.Tags
                .FirstOrDefaultAsync(t => t.Code == code.ToLower().Trim().Replace(" ", "-"), ct);

       
        /// Lấy toàn bộ Tag do Expert tạo, kèm trạng thái PublishRequest, mới nhất trước.
            public async Task<List<Tag>> GetByExpertAsync(Guid expertId, CancellationToken ct = default)
            => await _context.Tags
                .Include(t => t.PublishRequest)
                .Where(t => t.CreatedById == expertId)
                .OrderByDescending(t => t.CreatedAt)
                .AsNoTracking()
                .ToListAsync(ct);

        // ===========================
        // UC-85
        // ===========================

        public async Task<List<Tag>> GetPendingVerificationRequestsAsync(
            CancellationToken ct = default)
            => await _context.Tags
                .Include(t => t.PublishRequest)
                .Where(t => !t.IsPublished
                         && t.PublishRequest != null)
                .OrderByDescending(t => t.PublishRequest!.CreatedAt)
                .AsNoTracking()
                .ToListAsync(ct);

        public async Task<Tag?> GetVerificationRequestByIdAsync(
     Guid tagId,
     CancellationToken ct = default)
     => await _context.Tags
         .Include(t => t.PublishRequest)
         .FirstOrDefaultAsync(
             t => t.Id == tagId
               && !t.IsPublished
               && t.PublishRequest != null,
             ct);

        // ===========================
        // UC-86 ~ UC-88
        // ===========================

        public async Task<List<Tag>> GetSystemTagsAsync(
            CancellationToken ct = default)
            => await _context.Tags
                .Where(t => t.CreatedById == null && t.IsPublished)
                .OrderBy(t => t.Name)
                .AsNoTracking()
                .ToListAsync(ct);

        public async Task<bool> IsAssignedToCourseAsync(
            Guid tagId,
            CancellationToken ct = default)
            => await _context.Courses
                .AnyAsync(c => c.CourseTags.Any(t => t.Id == tagId), ct);


        public async Task<int> GetUsageCountAsync(
    Guid tagId,
    CancellationToken ct = default)
        {
            return await _context.Courses
                .CountAsync(c => c.CourseTags.Any(t => t.Id == tagId), ct);
        }
    }
}

