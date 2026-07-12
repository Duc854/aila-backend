using System;
using AILA.Domain.Enums;

namespace AILA.Application.Features.Users.Dtos
{
    public class UserListDto
    {
        public Guid Id { get; init; }
        public string Email { get; init; } = string.Empty;
        public string FullName { get; init; } = string.Empty;
        public UserRole Role { get; init; }
        public bool IsActive { get; init; }  // Dùng bool thay vì AccountStatus enum
        public DateTime CreatedAt { get; init; }
        public string? AvatarUrl { get; init; }
    }
}