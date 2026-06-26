using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Common.Dtos
{
    public class MaterialDetailDto
    {
        public Guid Id { get; set; }
        public Guid ModuleId { get; set; }
        public string Title { get; set; } = null!;
        public string MaterialType { get; set; } = null!;
        public int OrderIndex { get; set; }

        // Chi tiết động dựa theo loại học liệu
        public VideoMaterialDto? VideoDetails { get; set; }
        public DocumentMaterialDto? DocumentDetails { get; set; }
    }

    public class VideoMaterialDto
    {
        public string VideoUrl { get; set; } = null!;
        public string? ThumbnailUrl { get; set; }
        public string? CaptionsUrl { get; set; }
        public string? Content { get; set; }
    }

    public class DocumentMaterialDto
    {
        public string? Content { get; set; }
        public string? DocumentUrl { get; set; }
    }
}
