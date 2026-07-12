namespace AILA.Application.Common.Dtos
{
    public class ExpertProfileDto
    {
        public Guid    UserId             { get; set; }
        public string  FullName           { get; set; } = string.Empty;
        public string  Email              { get; set; } = string.Empty;
        public string? AvatarUrl          { get; set; }
        public string? Bio                { get; set; }
        public string? Specialty          { get; set; }
        public int     YearsOfExperience  { get; set; }
    }
}
