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

        public async Task<bool> IsMaterialInCourseAsync(Guid materialId, Guid courseId, CancellationToken cancellationToken = default)
        {
            // Kiểm tra thông qua bảng Module trung gian (Material -> Module -> Course)
            return await _context.Materials
                .AnyAsync(m => m.Id == materialId && m.Module.CourseId == courseId, cancellationToken);
        }

        public async Task<Material?> GetWithModuleAndCourseAsync(
            Guid materialId,
            CancellationToken cancellationToken = default)
        {
            return await _context.Materials
                .Include(m => m.Module)
                    .ThenInclude(m => m.Course)
                .FirstOrDefaultAsync(
                    m => m.Id == materialId,
                    cancellationToken);
        }

        public async Task<List<Material>> GetByModuleIdAsync(
            Guid moduleId,
            CancellationToken cancellationToken = default)
        {
            return await _context.Materials
                .Where(m => m.ModuleId == moduleId)
                .OrderBy(m => m.OrderIndex)
                .ToListAsync(cancellationToken);
        }

        public async Task<VideoMaterial?> GetVideoDetailForExpertAsync(
            Guid materialId,
            CancellationToken cancellationToken = default)
        {
            return await _context.VideoMaterials
                .Include(v => v.Material)
                    .ThenInclude(m => m.Module)
                        .ThenInclude(m => m.Course)
                .FirstOrDefaultAsync(
                    v => v.MaterialId == materialId,
                    cancellationToken);
        }

        public async Task<DocumentMaterial?> GetDocumentDetailForExpertAsync(
            Guid materialId,
            CancellationToken cancellationToken = default)
        {
            return await _context.DocumentMaterials
                .Include(d => d.Material)
                    .ThenInclude(m => m.Module)
                        .ThenInclude(m => m.Course)
                .FirstOrDefaultAsync(
                    d => d.MaterialId == materialId,
                    cancellationToken);
        }

        public async Task<QuizMaterial?> GetQuizDetailForExpertAsync(
            Guid materialId,
            CancellationToken cancellationToken = default)
        {
            return await _context.QuizMaterials
                .Include(q => q.Material)
                    .ThenInclude(m => m.Module)
                        .ThenInclude(m => m.Course)
                .FirstOrDefaultAsync(
                    q => q.MaterialId == materialId,
                    cancellationToken);
        }
    }
}
