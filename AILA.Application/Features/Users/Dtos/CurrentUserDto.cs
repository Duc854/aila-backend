namespace AILA.Application.Features.Users.Dtos
{
    public class CurrentUserDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}
