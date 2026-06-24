namespace AILA.Application.Common.Dtos
{
    public class NotificationDto
    {
        public Guid    Id          { get; set; }
        public string  Title       { get; set; } = string.Empty;
        public string  Body        { get; set; } = string.Empty;
        public bool    IsRead      { get; set; }
        public DateTime? ReadAt    { get; set; }
        public string  Type        { get; set; } = string.Empty;
        public string? RedirectUrl { get; set; }
        public DateTime CreatedAt  { get; set; }
    }
}
