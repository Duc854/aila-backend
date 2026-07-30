using System.Net;
using AILA.Application.Common.Interfaces;

namespace AILA.Infrastructure.Services.Email
{
    /// <summary>
    /// Cài đặt <see cref="IEmailSender"/> cho tầng Application: dựng nội dung email rồi
    /// đẩy vào hàng đợi và trả về ngay. Việc gửi thật do <see cref="EmailBackgroundService"/> lo.
    /// </summary>
    public sealed class QueuedEmailSender : IEmailSender
    {
        private readonly IEmailQueue _queue;

        public QueuedEmailSender(IEmailQueue queue)
        {
            _queue = queue;
        }

        public async Task SendPasswordResetOtpAsync(
            string toEmail,
            string fullName,
            string otp,
            int expiresInMinutes,
            CancellationToken cancellationToken = default)
        {
            var message = new EmailMessage(
                ToEmail: toEmail,
                ToName: fullName,
                Subject: "Mã OTP đặt lại mật khẩu AILA",
                HtmlBody: BuildHtmlBody(fullName, otp, expiresInMinutes),
                TextBody: BuildTextBody(fullName, otp, expiresInMinutes));

            await _queue.EnqueueAsync(message, cancellationToken);
        }

        private static string BuildHtmlBody(string fullName, string otp, int expiresInMinutes)
        {
            // Tên người dùng do người dùng tự nhập nên phải encode trước khi nhúng vào HTML.
            var safeName = WebUtility.HtmlEncode(fullName);

            return $"""
                <div style="font-family:Segoe UI,Arial,sans-serif;font-size:15px;color:#1f2933;">
                  <p>Xin chào {safeName},</p>
                  <p>Bạn vừa yêu cầu đặt lại mật khẩu cho tài khoản AILA. Mã OTP của bạn là:</p>
                  <p style="font-size:30px;font-weight:700;letter-spacing:6px;margin:24px 0;">{otp}</p>
                  <p>Mã có hiệu lực trong <strong>{expiresInMinutes} phút</strong> và chỉ dùng được một lần.</p>
                  <p>Nếu bạn không yêu cầu đặt lại mật khẩu, hãy bỏ qua email này — mật khẩu hiện tại của bạn vẫn an toàn.</p>
                  <p style="color:#7b8794;font-size:13px;margin-top:32px;">Email được gửi tự động, vui lòng không trả lời.</p>
                </div>
                """;
        }

        private static string BuildTextBody(string fullName, string otp, int expiresInMinutes)
            => $"""
                Xin chào {fullName},

                Mã OTP đặt lại mật khẩu AILA của bạn là: {otp}
                Mã có hiệu lực trong {expiresInMinutes} phút và chỉ dùng được một lần.

                Nếu bạn không yêu cầu đặt lại mật khẩu, hãy bỏ qua email này.
                """;
    }
}
