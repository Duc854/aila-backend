using AILA.Application.Features.Auth.DTOs;
using MediatR;

namespace AILA.Application.Features.Auth.Commands
{
    public class GoogleLoginCommand : IRequest<AuthResponse>
    {
        public string IdToken { get; set; } = string.Empty;
    }
}
