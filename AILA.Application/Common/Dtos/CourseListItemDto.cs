namespace AILA.Application.Common.Dtos
{
    public class CourseListItemDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ThumbnailUrl { get; set; }
        public string Level { get; set; } = string.Empty;
        public decimal DurationHours { get; set; }
        public CategoryDto Category { get; set; } = new();
        public AuthorDto Author { get; set; } = new();
        public List<TagDto> Tags { get; set; } = [];
    }
}
