using AILA.Application.Common.Interfaces.Repositories;
using AILA.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AILA.Infrastructure.Persistence.Repositories
{
    public class ExpertRepository(ApplicationDbContext context)
        : GenericRepository<Expert>(context), IExpertRepository
    {
        public async Task<Expert?> GetWithUserAsync(Guid userId, CancellationToken ct = default)
            => await _context.Experts
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.UserId == userId, ct);

        public async Task<Expert?> GetReadonlyWithUserAsync(Guid userId, CancellationToken ct = default)
            => await _context.Experts
                .AsNoTracking()
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.UserId == userId, ct);
    }
}
