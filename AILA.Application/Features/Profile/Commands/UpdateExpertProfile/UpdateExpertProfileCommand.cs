using AILA.Application.Features.Profile.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Profile.Commands.UpdateExpertProfile
{
    public record UpdateExpertProfileCommand(
        Guid UserId,
        string FullName,
        string? AvatarUrl,
        string? Bio,
        string? Specialty,
        int YearsOfExperience
    ) : IRequest<ResponseDto<ExpertProfileDto>>;
}
