namespace AILA.Application.Common.Dtos
{
    public class PublicExpertProfileDto
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string? Bio { get; set; }
        public string? Specialty { get; set; }
        public int YearsOfExperience { get; set; }
        public List<PublicCourseDto> Courses { get; set; } = [];
        public int TotalPublishedCourses { get; set; }
    }

    public class PublicCourseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ThumbnailUrl { get; set; }
        public string Level { get; set; } = string.Empty;
        public decimal DurationHours { get; set; }
    }
}
