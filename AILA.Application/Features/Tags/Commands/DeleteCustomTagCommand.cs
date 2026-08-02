using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Tags.Commands
{
    /// <summary>
    /// Expert xóa tag do mình tạo, chưa được publish và không đang được gán vào khóa học nào.
    /// </summary>
    public record DeleteCustomTagCommand(
        Guid TagId,
        Guid ExpertId
    ) : IRequest<ResponseDto<object>>;
}
