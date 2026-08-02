using AILA.Application.Common.Interfaces.Repositories;
using AILA.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AILA.Infrastructure.Persistence.Repositories
{
    public class CategoryRepository : GenericRepository<Category>, ICategoryRepository
    {
        public CategoryRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<List<Category>> GetActiveCategoriesAsync()
        {
            return await _context.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.OrderIndex)
                .AsNoTracking()
                .ToListAsync();
        }
        public async Task<IEnumerable<Category>> GetAllOrderedAsync(
           CancellationToken cancellationToken = default)
        {
            return await _context.Categories
                .AsNoTracking()
                .OrderBy(c => c.OrderIndex)
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> ExistsByNameAsync(
            string name,
            CancellationToken cancellationToken = default)
        {
            return await _context.Categories
                .AsNoTracking()
                .AnyAsync(c =>
                    c.Name.ToLower() == name.Trim().ToLower(),
                    cancellationToken);
        }

        public async Task<bool> ExistsByNameExceptIdAsync(
            Guid categoryId,
            string name,
            CancellationToken cancellationToken = default)
        {
            return await _context.Categories
                .AsNoTracking()
                .AnyAsync(c =>
                    c.Id != categoryId &&
                    c.Name.ToLower() == name.Trim().ToLower(),
                    cancellationToken);
        }

        public async Task<bool> HasCoursesAsync(
            Guid categoryId,
            CancellationToken cancellationToken = default)
        {
            return await _context.Courses
                .AsNoTracking()
                .AnyAsync(c => c.CategoryId == categoryId,
                    cancellationToken);
        }

        public async Task<List<Category>> GetByIdsAsync(
            IEnumerable<Guid> ids,
            CancellationToken cancellationToken = default)
        {
            return await _context.Categories
                .Where(c => ids.Contains(c.Id))
                .ToListAsync(cancellationToken);
        }
    }

}

