using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Common.Dtos
{
    public class CourseContentDto
    {
        public Guid CourseId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ThumbnailUrl { get; set; }
        public decimal ProgressPct { get; set; }
        public string EnrollmentStatus { get; set; } = string.Empty;
        public List<ModuleContentDto> Modules { get; set; } = new();
    }

    public class ModuleContentDto
    {
        public Guid ModuleId { get; set; }
        public string Title { get; set; } = string.Empty;
        public int OrderIndex { get; set; }
        public List<MaterialContentDto> Materials { get; set; } = new();
    }

    public class MaterialContentDto
    {
        public Guid MaterialId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string MaterialType { get; set; } = string.Empty;
        public int OrderIndex { get; set; }
        public bool IsCompleted { get; set; }
        public VideoDetailDto? VideoDetails { get; set; }
        public DocumentDetailDto? DocumentDetails { get; set; }
    }

    public class VideoDetailDto
    {
        public string VideoUrl { get; set; } = string.Empty;
        public string? ThumbnailUrl { get; set; }
        public int DurationSeconds { get; set; }
        public string? Content { get; set; }
        public string? CaptionsUrl { get; set; }
    }

    public class DocumentDetailDto
    {
        public string? DocumentUrl { get; set; }
        public string Content { get; set; } = string.Empty;
        public int? FileSizeKb { get; set; }
    }
}
