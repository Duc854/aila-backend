namespace AILA.Application.Common.Dtos
{
    public class AuthorDto
    {
        public Guid UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string? Specialty { get; set; }
        public string? Bio { get; set; }
        public int YearsOfExperience { get; set; }
    }
}
