using MediatR;
using System;

namespace AILA.Application.Features.Auth.Commands
{
    public class RegisterCommand : IRequest<Guid>
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
    }
}
