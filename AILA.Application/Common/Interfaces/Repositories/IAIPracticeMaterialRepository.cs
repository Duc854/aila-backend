using AILA.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Common.Interfaces.Repositories
{
    public interface IAIPracticeMaterialRepository
        : IGenericRepository<AIPracticeMaterial>
    {
        Task<AIPracticeMaterial?> GetDetailForExpertAsync(
            Guid materialId,
            CancellationToken cancellationToken = default);

        Task<AIPracticeMaterial?> GetByIdWithDetailsAsync(
            Guid materialId,
            CancellationToken cancellationToken = default);

        Task<AIPracticeMaterial?> GetForUpdateAsync(
            Guid materialId,
            CancellationToken cancellationToken = default);

        Task DeletePromptTemplatesAsync(
            Guid materialId,
            CancellationToken cancellationToken = default);

        Task DeleteStepGuidancesAsync(
            Guid materialId,
            CancellationToken cancellationToken = default);

        Task DeleteScoringCriteriaAsync(
            Guid materialId,
            CancellationToken cancellationToken = default);
    }
}
