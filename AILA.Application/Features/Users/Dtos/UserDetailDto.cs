using AILA.Application.Features.Profile.Dtos;
using AILA.Domain.Enums;
using System;

namespace AILA.Application.Features.Users.Dtos
{
    public class UserDetailDto
    {
        public Guid Id { get; init; }
        public string Email { get; init; } = string.Empty;
        public string FullName { get; init; } = string.Empty;
        public UserRole Role { get; init; }
        public bool IsActive { get; init; }
        public string? AvatarUrl { get; init; }
        public string? GoogleId { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime? UpdatedAt { get; init; }

        // Expert info (nếu có)
        public ExpertProfileDto? ExpertProfile { get; init; }
        // Learner info (nếu có)
        public LearnerProfileDto? LearnerProfile { get; init; }
    }
}
