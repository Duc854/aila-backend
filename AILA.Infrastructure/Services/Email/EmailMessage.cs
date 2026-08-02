namespace AILA.Infrastructure.Services.Email
{
    /// <summary>Một email đang chờ gửi trong hàng đợi nền.</summary>
    /// <param name="ToEmail">Địa chỉ nhận.</param>
    /// <param name="ToName">Tên hiển thị của người nhận.</param>
    /// <param name="Subject">Tiêu đề.</param>
    /// <param name="HtmlBody">Nội dung HTML.</param>
    /// <param name="TextBody">Nội dung plaintext dự phòng.</param>
    public sealed record EmailMessage(
        string ToEmail,
        string ToName,
        string Subject,
        string HtmlBody,
        string TextBody);
}
