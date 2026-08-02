namespace AILA.Application.Common.Models
{
    /// <summary>
    /// Bản ghi OTP đang active của một email, đọc ra từ store (Redis HASH).
    /// </summary>
    /// <param name="OtpHash">Hash của OTP (không phải OTP thô).</param>
    /// <param name="AttemptCount">Số lần verify sai đã ghi nhận trên OTP này.</param>
    public sealed record OtpEntry(string OtpHash, int AttemptCount);
}
