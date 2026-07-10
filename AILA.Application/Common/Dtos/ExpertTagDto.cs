namespace AILA.Application.Common.Dtos
{
    /// DTO Tag dành cho Expert: bao gồm trạng thái publish và yêu cầu xét duyệt.
    public class ExpertTagDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public bool IsPublished { get; set; }
        public DateTime CreatedAt { get; set; }

        /// Null nếu chưa có yêu cầu xét duyệt nào
        public TagPublishRequestDto? PublishRequest { get; set; }
    }

    public class TagPublishRequestDto
    {
        public Guid Id { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
    }
}
