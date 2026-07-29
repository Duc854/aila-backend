using AILA.Application.Features.Tags.Dtos;
using MediatR;

namespace AILA.Application.Features.Tags.Queries
{
    /// <summary>
    /// Lấy thông tin tag theo code slug.
    /// Trả về TagDto nếu tìm thấy, null nếu không tồn tại.
    /// </summary>
    public record GetTagByCodeQuery(string Code) : IRequest<TagDto?>;
}
