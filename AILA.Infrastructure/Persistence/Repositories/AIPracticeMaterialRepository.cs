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
    public class AIPracticeMaterialRepository
        : GenericRepository<AIPracticeMaterial>,
          IAIPracticeMaterialRepository
    {
        public AIPracticeMaterialRepository(ApplicationDbContext context)
            : base(context)
        {
        }

        public async Task<AIPracticeMaterial?> GetByIdWithDetailsAsync(Guid materialId, CancellationToken cancellationToken = default)
        {
            return await _context.AIPracticeMaterials
                .Include(x => x.PromptTemplates)
                .Include(x => x.StepGuidances)
                .Include(x => x.ScoringCriterias)
                .FirstOrDefaultAsync(x => x.MaterialId == materialId, cancellationToken);
        }

        public async Task<AIPracticeMaterial?> GetDetailForExpertAsync(Guid materialId,CancellationToken cancellationToken = default)
        {
            return await _context.AIPracticeMaterials
                .Include(x => x.Material)
                    .ThenInclude(x => x.Module)
                        .ThenInclude(x => x.Course)

                .Include(x => x.PromptTemplates)

                .Include(x => x.StepGuidances)

                .Include(x => x.ScoringCriterias)

                .FirstOrDefaultAsync(
                    x => x.MaterialId == materialId,
                    cancellationToken);
        }

        public async Task<AIPracticeMaterial?> GetForUpdateAsync(Guid materialId,CancellationToken cancellationToken = default)
        {
            return await _context.AIPracticeMaterials
                .Include(x => x.Material)
                    .ThenInclude(x => x.Module)
                        .ThenInclude(x => x.Course)

                .Include(x => x.PromptTemplates)

                .Include(x => x.StepGuidances)

                .Include(x => x.ScoringCriterias)

                .FirstOrDefaultAsync(
                    x => x.MaterialId == materialId,
                    cancellationToken);
        }

        public async Task DeletePromptTemplatesAsync(
    Guid materialId,
    CancellationToken cancellationToken = default)
        {
            var entities = await _context.PromptTemplates
                .Where(x => x.AIPracticeMaterialId == materialId)
                .ToListAsync(cancellationToken);

            _context.PromptTemplates.RemoveRange(entities);
        }

        public async Task DeleteStepGuidancesAsync(
            Guid materialId,
            CancellationToken cancellationToken = default)
        {
            var entities = await _context.StepGuidances
                .Where(x => x.AIPracticeMaterialId == materialId)
                .ToListAsync(cancellationToken);

            _context.StepGuidances.RemoveRange(entities);
        }

        public async Task DeleteScoringCriteriaAsync(
            Guid materialId,
            CancellationToken cancellationToken = default)
        {
            var entities = await _context.ScoringCriterias
                .Where(x => x.AIPracticeMaterialId == materialId)
                .ToListAsync(cancellationToken);

            _context.ScoringCriterias.RemoveRange(entities);
        }
    }
}
