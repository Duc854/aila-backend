using AILA.Application.Common.Dtos;
using MediatR;

namespace AILA.Application.Features.Experts.Queries
{
    /// <summary>
    /// Query: Lấy hồ sơ công khai của Expert (không yêu cầu xác thực).
    /// ExpertId chính là Experts.UserId (khóa chính của bảng Experts).
    /// </summary>
    public record GetPublicExpertProfileQuery(Guid ExpertId)
        : IRequest<PublicExpertProfileDto?>;
}
