using AILA.Application.Common.Dtos;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using Shared.Wrappers;

namespace AILA.Application.Common.Interfaces.Repositories
{
    public interface IUserRepository : IGenericRepository<User>
    {
        /// <summary>Tìm User theo email (không phân biệt hoa thường)</summary>
        Task<User?> GetByEmailAsync(string email);

        /// <summary>
        /// Lấy thông tin Expert kèm profile (User + Expert),
        /// dùng cho Expert Profile và Expert Login.
        /// </summary>
        Task<User?> GetExpertWithProfileAsync(Guid userId);

        #region UC-76: Review User Accounts

        /// <summary>
        /// UC-76: Lấy danh sách users với search và filter
        /// </summary>
        Task<List<User>> GetUsersAsync(
            string? searchKeyword = null,
            UserRole? role = null,
            bool? isActive = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// UC-76: Lấy danh sách users có phân trang
        /// </summary>
        Task<(List<User> Items, int TotalCount)> GetUsersPagedAsync(
            string? searchKeyword = null,
            UserRole? role = null,
            bool? isActive = null,
            int pageNumber = 1,
            int pageSize = 10,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// UC-76: Lấy chi tiết user theo Id
        /// </summary>
        Task<User?> GetUserByIdAsync(
            Guid userId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// UC-76: Lấy user với các details (Expert/Learner profile)
        /// </summary>
        Task<User?> GetUserWithDetailsAsync(
            Guid userId,
            CancellationToken cancellationToken = default);

        #endregion

        #region UC-77: Update User Status

        /// <summary>
        /// UC-77: Đếm số lượng Admin đang active (trừ user hiện tại)
        /// </summary>
        Task<int> CountActiveAdminsExceptAsync(
            Guid userId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// UC-77: Kiểm tra user có phải Admin không
        /// </summary>
        Task<bool> IsAdminAsync(
            Guid userId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// UC-77: Lấy role của user
        /// </summary>
        Task<UserRole?> GetUserRoleAsync(
            Guid userId,
            CancellationToken cancellationToken = default);

        #endregion

        #region UC-78: Create Expert Account

        /// <summary>
        /// UC-78: Kiểm tra email đã tồn tại chưa
        /// </summary>
        Task<bool> EmailExistsAsync(
            string email,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// UC-78: Lấy user với Expert profile
        /// </summary>
        Task<User?> GetUserWithExpertProfileAsync(
            Guid userId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// UC-78: Tạo user và expert profile trong 1 transaction
        /// </summary>
        Task<User> CreateUserWithExpertProfileAsync(
            User user,
            Expert expert,
            CancellationToken cancellationToken = default);

        #endregion

        #region Helper Methods

        /// <summary>
        /// Kiểm tra user có tồn tại không
        /// </summary>
        Task<bool> UserExistsAsync(
            Guid userId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Lấy user theo email (có tracking)
        /// </summary>
        Task<User?> GetByEmailWithTrackingAsync(
            string email,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Đếm tổng số users (có filter)
        /// </summary>
        Task<int> CountUsersAsync(
            string? searchKeyword = null,
            UserRole? role = null,
            bool? isActive = null,
            CancellationToken cancellationToken = default);

        #endregion

        Task<(List<AccountOverrideAccountDto> Items, int TotalItems)>GetOverrideEligibleAccountsAsync(
        string? keyword,
        PageRequest pageRequest,
        CancellationToken cancellationToken = default);

    }
}
