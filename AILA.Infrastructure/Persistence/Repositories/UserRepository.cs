using AILA.Application.Common.Interfaces.Repositories;
using AILA.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AILA.Infrastructure.Persistence.Repositories
{
    public class UserRepository : GenericRepository<User>, IUserRepository
    {
        public UserRepository(ApplicationDbContext context) : base(context) { }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == email.ToLower().Trim());
        }

        /// <summary>
        /// Eager-load User + Expert profile cùng lúc để tránh N+1 Query.
        /// Dùng cho Expert Login (kiểm tra role) và Expert Profile.
        /// </summary>
        public async Task<User?> GetExpertWithProfileAsync(Guid userId)
        {
            return await _context.Users
                .Include(u => u.Expert)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);
        }
    }
}
