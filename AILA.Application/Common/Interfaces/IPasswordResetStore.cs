using AILA.Application.Common.Models;

namespace AILA.Application.Common.Interfaces
{
    /// <summary>
    /// Nơi lưu state tạm thời của luồng Reset Password (OTP, reset token, attempt count,
    /// rate limit). Theo ràng buộc hạ tầng của UC-08: <b>không đụng schema DB</b> — toàn bộ
    /// state là ephemeral và hết hạn bằng TTL native của store (Redis).
    /// Mọi lỗi hạ tầng được gói thành <see cref="Exceptions.PasswordResetStoreUnavailableException"/>
    /// để tầng Application không phụ thuộc client cụ thể.
    /// </summary>
    public interface IPasswordResetStore
    {
        /// <summary>
        /// Ghi OTP hash cho email, kèm TTL. Ghi đè bản ghi cũ nếu có
        /// (BR-02: luôn chỉ 1 OTP active / email) và reset attemptCount về 0.
        /// </summary>
        Task SaveOtpAsync(string normalizedEmail, string otpHash, CancellationToken cancellationToken = default);

        /// <summary>Đọc OTP đang active. Trả null nếu đã hết hạn / đã bị xoá.</summary>
        Task<OtpEntry?> GetOtpAsync(string normalizedEmail, CancellationToken cancellationToken = default);

        /// <summary>
        /// Tăng attemptCount một cách atomic, giữ nguyên TTL. Trả về giá trị sau khi tăng,
        /// hoặc <c>-1</c> nếu bản ghi OTP không còn tồn tại.
        /// </summary>
        Task<int> IncrementOtpAttemptAsync(string normalizedEmail, CancellationToken cancellationToken = default);

        /// <summary>Xoá OTP (dùng sau khi verify thành công hoặc khi vượt ngưỡng brute-force).</summary>
        Task DeleteOtpAsync(string normalizedEmail, CancellationToken cancellationToken = default);

        /// <summary>Lưu reset token → userId, kèm TTL ngắn.</summary>
        Task SaveResetTokenAsync(string resetToken, Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Đọc userId của reset token mà <b>không</b> tiêu thụ token — dùng để validate
        /// trước khi đổi password, tránh đốt token oan (AC-7).
        /// </summary>
        Task<Guid?> PeekResetTokenAsync(string resetToken, CancellationToken cancellationToken = default);

        /// <summary>
        /// Tiêu thụ reset token một cách atomic (GETDEL): chỉ một caller lấy được userId,
        /// caller còn lại nhận null (AC-8, EDGE-10, EDGE-12).
        /// </summary>
        Task<Guid?> ConsumeResetTokenAsync(string resetToken, CancellationToken cancellationToken = default);

        /// <summary>
        /// Tăng bộ đếm rate limit cho một bucket ("email" / "ip") và trả về giá trị sau khi tăng.
        /// TTL của cửa sổ được đặt ngay lần tăng đầu tiên.
        /// </summary>
        Task<long> IncrementRateLimitAsync(string bucket, string identifier, CancellationToken cancellationToken = default);
    }
}
