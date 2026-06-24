using AILA.Application.Features.Profile.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Profile.Queries.GetLearnerProfile
{
    public record GetLearnerProfileQuery(Guid UserId) : IRequest<ResponseDto<LearnerProfileDto>>;
}
