using AILA.Application.Common.Dtos;
using MediatR;

namespace AILA.Application.Features.Tags.Commands
{
    /// Expert gửi yêu cầu xét duyệt (publish) một tag do mình tạo.
    /// Tag phải chưa được publish và chưa có request đang Pending.
    public record RequestTagVerificationCommand(
        Guid TagId,
        Guid ExpertId,
        string? Note
    ) : IRequest<ExpertTagDto>;
}
