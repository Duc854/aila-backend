using AILA.Application.Common.Dtos;
using MediatR;

namespace AILA.Application.Features.Tags.Commands
{
    /// <summary>
    /// Expert hủy yêu cầu xét duyệt đang Pending của một tag do mình tạo.
    /// </summary>
    public record DeleteTagPublishRequestCommand(
        Guid TagId,
        Guid ExpertId
    ) : IRequest<ExpertTagDto>;
}
