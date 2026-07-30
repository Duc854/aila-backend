using System.Security.Cryptography;
using System.Text;
using AILA.Application.Common.Exceptions;
using AILA.Application.Common.Interfaces;
using AILA.Application.Common.Models;
using Microsoft.Extensions.Options;
using Shared.Models;
using StackExchange.Redis;

namespace AILA.Infrastructure.Services
{
    /// <summary>
    /// Cài đặt store của luồng Reset Password trên Redis — thay cho bảng DB, đúng ràng buộc
    /// "không sửa schema" của UC-08.
    ///
    /// <para>Key schema:</para>
    /// <code>
    /// {prefix}otp:{sha256(email)}    HASH   { otpHash, attemptCount, createdAt }  EX OtpTtlSeconds
    /// {prefix}token:{sha256(token)}  STRING userId                                EX ResetTokenTtlSeconds
    /// {prefix}rl:email:{sha256(email)} STRING counter                             EX RateLimitWindowSeconds
    /// {prefix}rl:ip:{sha256(ip)}       STRING counter                             EX RateLimitWindowSeconds
    /// </code>
    ///
    /// <para>
    /// Email/IP/token đều được băm trước khi ghép vào tên key: dump Redis không lộ PII, và
    /// reset token — thứ tương đương bearer credential — không nằm plaintext trong keyspace.
    /// </para>
    /// <para>Hết hạn hoàn toàn do Redis TTL nên không cần job cleanup.</para>
    /// <para><b>Yêu cầu Redis 6.2+</b> (dùng lệnh GETDEL để consume token atomic).</para>
    /// </summary>
    public sealed class RedisPasswordResetStore : IPasswordResetStore
    {
        private const string OtpHashField = "otpHash";
        private const string AttemptCountField = "attemptCount";
        private const string CreatedAtField = "createdAt";

        /// <summary>
        /// HINCRBY trần sẽ tự tạo lại key (không TTL) nếu OTP vừa hết hạn giữa GET và INCR,
        /// để lại rác vĩnh viễn. Script này chỉ tăng khi bản ghi còn sống.
        /// </summary>
        private static readonly LuaScript IncrementAttemptScript = LuaScript.Prepare(
            @"if redis.call('EXISTS', @key) == 0 then return -1 end
              return redis.call('HINCRBY', @key, @field, 1)");

        /// <summary>
        /// INCR rồi EXPIRE ở lần đầu tiên — gói trong một script để cửa sổ rate limit
        /// không bao giờ rơi vào trạng thái "có counter nhưng không có TTL".
        /// </summary>
        private static readonly LuaScript IncrementRateLimitScript = LuaScript.Prepare(
            @"local current = redis.call('INCR', @key)
              if current == 1 then redis.call('EXPIRE', @key, @ttl) end
              return current");

        private readonly IConnectionMultiplexer _redis;
        private readonly PasswordResetSettings _settings;

        public RedisPasswordResetStore(
            IConnectionMultiplexer redis,
            IOptions<PasswordResetSettings> settings)
        {
            _redis = redis;
            _settings = settings.Value;
        }

        private IDatabase Db => _redis.GetDatabase();

        private string Prefix => string.IsNullOrWhiteSpace(_settings.RedisKeyPrefix)
            ? "pwdreset:"
            : _settings.RedisKeyPrefix;

        private RedisKey OtpKey(string normalizedEmail) => $"{Prefix}otp:{Digest(normalizedEmail)}";
        private RedisKey TokenKey(string resetToken) => $"{Prefix}token:{Digest(resetToken)}";
        private RedisKey RateLimitKey(string bucket, string identifier)
            => $"{Prefix}rl:{bucket}:{Digest(identifier)}";

        private static string Digest(string value)
            => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

        /// <inheritdoc />
        public Task SaveOtpAsync(string normalizedEmail, string otpHash, CancellationToken cancellationToken = default)
            => GuardAsync(async () =>
            {
                var key = OtpKey(normalizedEmail);
                var ttl = TimeSpan.FromSeconds(_settings.OtpTtlSeconds);

                // MULTI/EXEC: xoá bản ghi cũ rồi ghi bản mới + TTL trong một lượt.
                // Việc xoá trước là cố ý — nó reset attemptCount, nếu không thì OTP mới sẽ
                // thừa hưởng số lần thử sai của OTP cũ (BR-02: mỗi OTP là một lượt mới).
                var transaction = Db.CreateTransaction();

                _ = transaction.KeyDeleteAsync(key);
                _ = transaction.HashSetAsync(key, new[]
                {
                    new HashEntry(OtpHashField, otpHash),
                    new HashEntry(AttemptCountField, 0),
                    new HashEntry(CreatedAtField, DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                });
                _ = transaction.KeyExpireAsync(key, ttl);

                return await transaction.ExecuteAsync();
            });

        /// <inheritdoc />
        public Task<OtpEntry?> GetOtpAsync(string normalizedEmail, CancellationToken cancellationToken = default)
            => GuardAsync(async () =>
            {
                var entries = await Db.HashGetAllAsync(OtpKey(normalizedEmail));
                if (entries.Length == 0)
                    return null;

                var otpHash = entries.FirstOrDefault(e => e.Name == OtpHashField).Value;
                if (!otpHash.HasValue)
                    return null;

                var attemptRaw = entries.FirstOrDefault(e => e.Name == AttemptCountField).Value;
                var attemptCount = attemptRaw.HasValue && int.TryParse(attemptRaw!, out var parsed)
                    ? parsed
                    : 0;

                return new OtpEntry(otpHash!, attemptCount);
            });

        /// <inheritdoc />
        public Task<int> IncrementOtpAttemptAsync(string normalizedEmail, CancellationToken cancellationToken = default)
            => GuardAsync(async () =>
            {
                var result = await Db.ScriptEvaluateAsync(
                    IncrementAttemptScript,
                    new { key = OtpKey(normalizedEmail), field = (RedisValue)AttemptCountField });

                return (int)result;
            });

        /// <inheritdoc />
        public Task DeleteOtpAsync(string normalizedEmail, CancellationToken cancellationToken = default)
            => GuardAsync(() => Db.KeyDeleteAsync(OtpKey(normalizedEmail)));

        /// <inheritdoc />
        public Task SaveResetTokenAsync(string resetToken, Guid userId, CancellationToken cancellationToken = default)
            => GuardAsync(() => Db.StringSetAsync(
                TokenKey(resetToken),
                userId.ToString(),
                TimeSpan.FromSeconds(_settings.ResetTokenTtlSeconds)));

        /// <inheritdoc />
        public Task<Guid?> PeekResetTokenAsync(string resetToken, CancellationToken cancellationToken = default)
            => GuardAsync(async () => ParseUserId(await Db.StringGetAsync(TokenKey(resetToken))));

        /// <inheritdoc />
        public Task<Guid?> ConsumeResetTokenAsync(string resetToken, CancellationToken cancellationToken = default)
            => GuardAsync(async () => ParseUserId(await Db.StringGetDeleteAsync(TokenKey(resetToken))));

        /// <inheritdoc />
        public Task<long> IncrementRateLimitAsync(string bucket, string identifier, CancellationToken cancellationToken = default)
            => GuardAsync(async () =>
            {
                var result = await Db.ScriptEvaluateAsync(
                    IncrementRateLimitScript,
                    new
                    {
                        key = RateLimitKey(bucket, identifier),
                        ttl = (RedisValue)_settings.RateLimitWindowSeconds
                    });

                return (long)result;
            });

        private static Guid? ParseUserId(RedisValue value)
            => value.HasValue && Guid.TryParse(value!, out var userId) ? userId : null;

        // --- EDGE-13: mọi sự cố Redis được quy về một exception của tầng Application,
        //     để handler trả 503 thay vì để lộ chi tiết hạ tầng hoặc rơi vào 500 chung. ---

        private static async Task<T> GuardAsync<T>(Func<Task<T>> action)
        {
            try
            {
                return await action();
            }
            catch (Exception ex) when (IsRedisFailure(ex))
            {
                throw new PasswordResetStoreUnavailableException(
                    "Không kết nối được tới Redis cho luồng reset password.", ex);
            }
        }

        // RedisConnectionException và RedisTimeoutException đều kế thừa RedisException.
        private static bool IsRedisFailure(Exception ex)
            => ex is RedisException or ObjectDisposedException;
    }
}
