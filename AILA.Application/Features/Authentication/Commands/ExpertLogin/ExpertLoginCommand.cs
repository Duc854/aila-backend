using AILA.Application.Features.Authentication.Dtos;
using MediatR;

namespace AILA.Application.Features.Authentication.Commands.ExpertLogin
{
    public record ExpertLoginCommand(string Email, string Password)
        : IRequest<LoginResponseDto?>;
}
