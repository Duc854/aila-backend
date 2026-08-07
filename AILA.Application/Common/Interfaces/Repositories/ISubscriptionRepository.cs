using AILA.Domain.Entities;
using AILA.Domain.Enums;
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

        /// <summary>
        /// UC-21 / BR-03: Lấy thông tin sử dụng tài nguyên hiện tại của tài khoản
        /// </summary>
        Task<AccountResourceUsage?> GetResourceUsageByAccountIdAsync(
            Guid accountId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// UC-21: Lấy thông tin hạn mức ghi đè riêng của tài khoản (nếu có)
        /// </summary>
        Task<AccountResourceLimit?> GetResourceLimitByAccountIdAsync(
            Guid accountId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// UC-21 / AF-01: Lấy hạn mức chính sách mặc định của nền tảng khi học viên không có gói active
        /// </summary>
        Task<ResourceLimitPolicy?> GetDefaultPolicyAsync(
            ResourceAccountType accountType = ResourceAccountType.Learner,
            CancellationToken cancellationToken = default);
    }
}
