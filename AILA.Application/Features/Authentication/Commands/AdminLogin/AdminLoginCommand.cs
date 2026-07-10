using AILA.Application.Features.Authentication.Dtos;
using MediatR;

namespace AILA.Application.Features.Authentication.Commands.AdminLogin
{
    public record AdminLoginCommand(string Email, string Password)
        : IRequest<LoginResponseDto?>;
}
