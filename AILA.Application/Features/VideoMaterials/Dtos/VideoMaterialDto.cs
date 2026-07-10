using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Features.VideoMaterials.Dtos
{
    public sealed class VideoMaterialDto
    {
        public Guid MaterialId { get; init; }

        public string Title { get; init; } = string.Empty;

        public string VideoUrl { get; init; } = string.Empty;

        public int DurationSeconds { get; init; }

        public string? Content { get; init; }
    }
}
