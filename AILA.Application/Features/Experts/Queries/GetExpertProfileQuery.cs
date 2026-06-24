using AILA.Application.Common.Dtos;
using MediatR;

namespace AILA.Application.Features.Experts.Queries
{
    /// <summary>
    /// Query: Lấy thông tin profile của Expert đang đăng nhập.
    /// ExpertId chính là UserId lấy từ JWT claim.
    /// </summary>
    public record GetExpertProfileQuery(Guid ExpertId)
        : IRequest<ExpertProfileDto?>;
}
