using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Features.AdminBlog.DTOs
{
    public class AdminBlogListItemDto
    {
        public Guid Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Slug { get; set; } = string.Empty;

        public string? ThumbnailUrl { get; set; }

        public bool IsPublished { get; set; }

        public int ViewCount { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? PublishedAt { get; set; }
    }
}
