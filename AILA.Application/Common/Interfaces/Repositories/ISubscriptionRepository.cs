using AILA.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AILA.Application.Common.Interfaces.Repositories
{
    public interface ISubscriptionRepository : IGenericRepository<Subscription>
    {
        /// <summary>
        /// UC-21 / BR-02: Lấy gói đăng ký Active còn hiệu lực của Learner
        /// </summary>
        Task<Subscription?> GetActiveSubscriptionByLearnerIdAsync(
            Guid learnerId,
            CancellationToken cancellationToken = default);
    }
}
