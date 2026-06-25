using System;

namespace AILA.Application.Features.Blogs.DTOs
{
    public class BlogDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? ThumbnailUrl { get; set; }
        public DateTime? PublishedAt { get; set; }
    }
}
