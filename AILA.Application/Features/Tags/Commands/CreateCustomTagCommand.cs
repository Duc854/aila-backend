using AILA.Application.Common.Dtos;
using MediatR;

namespace AILA.Application.Features.Tags.Commands
{
    /// Expert tạo tag riêng. Tag sẽ ở trạng thái chưa duyệt (IsPublished = false)
    /// cho đến khi Expert gửi yêu cầu xét duyệt và Admin phê duyệt.
    public record CreateCustomTagCommand(
        Guid ExpertId,
        string Name,
        string Code
    ) : IRequest<ExpertTagDto>;
}
