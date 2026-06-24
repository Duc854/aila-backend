using AILA.Application.Common.Interfaces.Repositories;
using AILA.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AILA.Infrastructure.Persistence.Repositories
{
    public class TagRepository : GenericRepository<Tag>, ITagRepository
    {
        public TagRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<List<Tag>> GetPublishedTagsAsync()
        {
            return await _context.Tags
                .Where(t => t.IsPublished)
                .OrderBy(t => t.Name)
                .AsNoTracking()
                .ToListAsync();
        }
    }
}
