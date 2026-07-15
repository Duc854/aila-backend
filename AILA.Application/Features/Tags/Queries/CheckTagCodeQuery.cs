using MediatR;

namespace AILA.Application.Features.Tags.Queries
{
    /// <summary>
    /// Kiểm tra code tag đã tồn tại trong hệ thống chưa (tránh trùng slug).
    /// Trả về true nếu đã tồn tại, false nếu còn trống.
    /// </summary>
    public record CheckTagCodeQuery(string Code) : IRequest<bool>;
}
