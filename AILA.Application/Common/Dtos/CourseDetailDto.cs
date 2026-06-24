namespace AILA.Application.Common.Dtos
{
    public class CourseDetailDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ThumbnailUrl { get; set; }
        public string Level { get; set; } = string.Empty;
        public decimal DurationHours { get; set; }
        public bool IsPublished { get; set; }
        public CategoryDto Category { get; set; } = new();
        public AuthorDto Author { get; set; } = new();
        public List<TagDto> Tags { get; set; } = [];
        public List<ModuleDetailDto> Modules { get; set; } = [];
        public int TotalModules { get; set; }
        public int TotalMaterials { get; set; }
    }

    public class ModuleDetailDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int OrderIndex { get; set; }
        public bool IsPublished { get; set; }
        public List<MaterialDetailDto> Materials { get; set; } = [];
    }

    public class MaterialDetailDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public int OrderIndex { get; set; }
    }
}
