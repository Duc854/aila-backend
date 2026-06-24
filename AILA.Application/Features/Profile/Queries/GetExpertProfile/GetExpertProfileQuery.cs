using AILA.Application.Features.Profile.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Profile.Queries.GetExpertProfile
{
    public record GetExpertProfileQuery(Guid UserId) : IRequest<ResponseDto<ExpertProfileDto>>;
}
