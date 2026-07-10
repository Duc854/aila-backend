namespace AILA.Application.Common.Dtos
{
    /// <summary>
    /// DTO trả về sau khi tạo mới hoặc cập nhật khóa học.
    /// </summary>
    public class CourseManageResultDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ThumbnailUrl { get; set; }
        public string Level { get; set; } = string.Empty;
        public decimal DurationHours { get; set; }
        public bool IsPublished { get; set; }
        public Guid CategoryId { get; set; }
        public List<Guid> TagIds { get; set; } = [];
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
