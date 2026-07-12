using AILA.Domain.Enums;

namespace AILA.Application.Features.Users.Dtos
{
    public class RoleDto
    {
        public UserRole Value { get; init; }
        public string Name { get; init; } = string.Empty;
    }
}