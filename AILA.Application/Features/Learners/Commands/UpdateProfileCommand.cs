using MediatR;
using System;

namespace AILA.Application.Features.Learners.Commands
{
    public class UpdateProfileCommand : IRequest<Unit>
    {
        public Guid UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
    }
}
