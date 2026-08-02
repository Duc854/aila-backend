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

        /// Ghi chú của Expert khi gửi yêu cầu
        public string? RequestNote { get; set; }

        /// Phản hồi của Admin khi từ chối
        public string? ReviewComment { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
    }
}
