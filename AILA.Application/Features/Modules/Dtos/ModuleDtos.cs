namespace AILA.Application.Features.Modules.Dtos
{
    // ── RESPONSE ─────────────────────────────────────────────────────────────

    /// <summary>Thông tin một Chương học trả về cho Expert quản lý</summary>
    public class ModuleDto
    {
        public Guid    Id          { get; set; }
        public Guid    CourseId    { get; set; }
        public string  Title       { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int     OrderIndex  { get; set; }
        public bool    IsPublished { get; set; }
        public DateTime CreatedAt  { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int     MaterialCount { get; set; }
    }

    // ── REQUEST ───────────────────────────────────────────────────────────────

    /// <summary>Body khi tạo mới hoặc cập nhật thông tin chương</summary>
    public class SaveModuleRequest
    {
        public string  Title       { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int     OrderIndex  { get; set; }
    }

    /// <summary>Body khi đổi thứ tự nhiều chương cùng lúc (drag & drop)</summary>
    public class ReorderModulesRequest
    {
        /// <summary>Danh sách {ModuleId, NewOrderIndex} theo thứ tự mới</summary>
        public List<ModuleOrderItem> Items { get; set; } = new();
    }

    public class ModuleOrderItem
    {
        public Guid ModuleId     { get; set; }
        public int  NewOrderIndex { get; set; }
    }
}
