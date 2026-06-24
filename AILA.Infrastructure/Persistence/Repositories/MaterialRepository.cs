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
    public class MaterialRepository : GenericRepository<Material>, IMaterialRepository
    {
        private readonly ApplicationDbContext _context;

        public MaterialRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Material?> GetMaterialDetailAsync(Guid courseId, Guid materialId, CancellationToken cancellationToken = default)
        {
            return await _context.Materials
                .Include(m => m.Module)
                .Include(m => m.VideoDetails)
                .Include(m => m.DocumentDetails)
                .FirstOrDefaultAsync(m => m.Id == materialId && m.Module.CourseId == courseId, cancellationToken);
        }
    }
}
