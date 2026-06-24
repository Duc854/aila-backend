using AILA.Domain.Entities;

namespace AILA.Application.Common.Interfaces.Repositories
{
    public interface IExpertRepository : IGenericRepository<Expert>
    {
        Task<Expert?> GetWithUserAsync(Guid userId, CancellationToken ct = default);
        Task<Expert?> GetReadonlyWithUserAsync(Guid userId, CancellationToken ct = default);
    }
}
