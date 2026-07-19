using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Common.Interfaces.Repositories.AI
{
    public interface IQuotaService
    {
        Task EnsureWithinLimitAsync(Guid learnerId, ResourceType type, CancellationToken ct);
        Task ConsumeAsync(Guid learnerId, ResourceType type, int amount, CancellationToken ct);
    }

}
