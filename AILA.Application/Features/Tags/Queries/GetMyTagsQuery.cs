using AILA.Application.Common.Dtos;
using MediatR;

namespace AILA.Application.Features.Tags.Queries
{
    /// Lấy danh sách tag do Expert đang đăng nhập tạo, kèm trạng thái xét duyệt.
    public record GetMyTagsQuery(Guid ExpertId) : IRequest<List<ExpertTagDto>>;
}
