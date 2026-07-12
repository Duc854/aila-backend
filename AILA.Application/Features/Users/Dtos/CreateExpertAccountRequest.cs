using System;

namespace AILA.Application.Features.Users.Dtos
{
    public class CreateExpertAccountRequest
    {
        public string FullName { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string Password { get; init; } = string.Empty;
        public string? Bio { get; init; }
        public string? Specialty { get; init; }
        public int YearsOfExperience { get; init; }
        public string? AvatarUrl { get; init; }
    }
}