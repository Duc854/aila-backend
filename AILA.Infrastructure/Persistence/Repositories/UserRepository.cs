using AILA.Application.Common.Dtos;
using AILA.Application.Common.Interfaces.Repositories;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Shared.Wrappers;

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

        #region UC-76: Review User Accounts

        /// <summary>
        /// UC-76: Lấy danh sách users với search và filter
        /// </summary>
        public async Task<List<User>> GetUsersAsync(
            string? searchKeyword = null,
            UserRole? role = null,
            bool? isActive = null,
            CancellationToken cancellationToken = default)
        {
            var query = BuildFilterQuery(searchKeyword, role, isActive);

            return await query
                .OrderBy(u => u.FullName)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// UC-76: Lấy danh sách users có phân trang
        /// </summary>
        public async Task<(List<User> Items, int TotalCount)> GetUsersPagedAsync(
            string? searchKeyword = null,
            UserRole? role = null,
            bool? isActive = null,
            int pageNumber = 1,
            int pageSize = 10,
            CancellationToken cancellationToken = default)
        {
            var query = BuildFilterQuery(searchKeyword, role, isActive);

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderBy(u => u.FullName)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }

        /// <summary>
        /// UC-76: Lấy chi tiết user theo Id
        /// </summary>
        public async Task<User?> GetUserByIdAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        }

        /// <summary>
        /// UC-76: Lấy user với các details (Expert/Learner profile)
        /// </summary>
        public async Task<User?> GetUserWithDetailsAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .Include(u => u.Expert)
                .Include(u => u.Learner)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        }

        #endregion

        #region UC-77: Update User Status

        /// <summary>
        /// UC-77: Đếm số lượng Admin đang active (trừ user hiện tại)
        /// </summary>
        public async Task<int> CountActiveAdminsExceptAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .CountAsync(u => u.Role == UserRole.Admin
                                 && u.IsActive
                                 && u.Id != userId,
                            cancellationToken);
        }

        /// <summary>
        /// UC-77: Kiểm tra user có phải Admin không
        /// </summary>
        public async Task<bool> IsAdminAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .AnyAsync(u => u.Id == userId && u.Role == UserRole.Admin,
                         cancellationToken);
        }

        /// <summary>
        /// UC-77: Lấy role của user
        /// </summary>
        public async Task<UserRole?> GetUserRoleAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .Where(u => u.Id == userId)
                .Select(u => u.Role)
                .FirstOrDefaultAsync(cancellationToken);
        }

        #endregion

        #region UC-78: Create Expert Account

        /// <summary>
        /// UC-78: Kiểm tra email đã tồn tại chưa
        /// </summary>
        public async Task<bool> EmailExistsAsync(
            string email,
            CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .AnyAsync(u => u.Email == email.ToLower().Trim(),
                         cancellationToken);
        }

        /// <summary>
        /// UC-78: Lấy user với Expert profile
        /// </summary>
        public async Task<User?> GetUserWithExpertProfileAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .Include(u => u.Expert)
                .FirstOrDefaultAsync(u => u.Id == userId,
                                     cancellationToken);
        }

        /// <summary>
        /// UC-78: Tạo user và expert profile trong 1 transaction
        /// </summary>
        public async Task<User> CreateUserWithExpertProfileAsync(
            User user,
            Expert expert,
            CancellationToken cancellationToken = default)
        {
            using var transaction = await _context.Database
                .BeginTransactionAsync(cancellationToken);

            try
            {
                await _context.Users.AddAsync(user, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                await _context.Experts.AddAsync(expert, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                return user;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Kiểm tra user có tồn tại không
        /// </summary>
        public async Task<bool> UserExistsAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .AnyAsync(u => u.Id == userId, cancellationToken);
        }

        /// <summary>
        /// Lấy user theo email (có tracking)
        /// </summary>
        public async Task<User?> GetByEmailWithTrackingAsync(
            string email,
            CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email.ToLower().Trim(),
                                     cancellationToken);
        }

        /// <summary>
        /// Đếm tổng số users (có filter)
        /// </summary>
        public async Task<int> CountUsersAsync(
            string? searchKeyword = null,
            UserRole? role = null,
            bool? isActive = null,
            CancellationToken cancellationToken = default)
        {
            var query = BuildFilterQuery(searchKeyword, role, isActive);
            return await query.CountAsync(cancellationToken);
        }

        #endregion

        #region Private Helper

        /// <summary>
        /// Xây dựng query với filter (BR-02, BR-03, BR-04)
        /// </summary>
        private IQueryable<User> BuildFilterQuery(
            string? searchKeyword = null,
            UserRole? role = null,
            bool? isActive = null)
        {
            var query = _context.Users.AsQueryable();

            // BR-04: Exclude Admin accounts
            query = query.Where(u => u.Role != UserRole.Admin);

            // BR-02: Search by keyword (full name or email)
            if (!string.IsNullOrWhiteSpace(searchKeyword))
            {
                var keyword = searchKeyword.Trim().ToLower();
                query = query.Where(u =>
                    u.FullName.ToLower().Contains(keyword) ||
                    u.Email.ToLower().Contains(keyword));
            }

            // BR-03: Filter by role
            if (role.HasValue)
            {
                query = query.Where(u => u.Role == role.Value);
            }

            // BR-03: Filter by status
            if (isActive.HasValue)
            {
                query = query.Where(u => u.IsActive == isActive.Value);
            }

            return query;
        }

        #endregion

        public async Task<(List<AccountOverrideAccountDto> Items, int TotalItems)>
    GetOverrideEligibleAccountsAsync(
        string? keyword,
        PageRequest pageRequest,
        CancellationToken cancellationToken = default)
        {
            var query = _context.Users
                .AsNoTracking()
                .Where(x => x.Role != UserRole.Admin);


            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();

                query = query.Where(x =>
                    x.Email.Contains(keyword)
                    ||
                    x.FullName.Contains(keyword));
            }


            var totalItems = await query.CountAsync(
                cancellationToken);


            var items = await query
                .OrderBy(x => x.Email)

                .Skip(
                    pageRequest.PageIndex
                    * pageRequest.PageSize)

                .Take(pageRequest.PageSize)

                .Select(x => new AccountOverrideAccountDto
                {
                    AccountId = x.Id,

                    Email = x.Email,

                    FullName = x.FullName,

                    Role = x.Role.ToString(),

                    HasOverride = _context.AccountResourceLimits
                        .Any(r => r.AccountId == x.Id)
                })

                .ToListAsync(cancellationToken);


            return (items, totalItems);
        }
    }
}

