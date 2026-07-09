namespace AILA.Application.Common.Dtos
{
    /// <summary>
    /// DTO dành cho Expert xem danh sách khóa học của chính mình.
    /// Bao gồm thêm trạng thái publish và số lượng module/material.
    /// </summary>
    public class ExpertCourseListItemDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ThumbnailUrl { get; set; }
        public string Level { get; set; } = string.Empty;
        public decimal DurationHours { get; set; }
        public bool IsPublished { get; set; }
        public CategoryDto Category { get; set; } = new();
        public List<TagDto> Tags { get; set; } = [];
        public int TotalModules { get; set; }
        public int TotalMaterials { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
