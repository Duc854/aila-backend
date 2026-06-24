using AILA.Application.Common.Interfaces.Repositories;
using AILA.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AILA.Infrastructure.Persistence.Repositories
{
    public class TagRepository(ApplicationDbContext context)
        : GenericRepository<Tag>(context), ITagRepository
    {
        public async Task<List<Tag>> GetByIdsAsync(IEnumerable<Guid> tagIds, CancellationToken ct = default)
            => await _context.Tags
                .Where(t => tagIds.Contains(t.Id))
                .ToListAsync(ct);
    }
}
