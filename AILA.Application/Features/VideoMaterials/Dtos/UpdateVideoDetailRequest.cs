using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Features.VideoMaterials.Dtos
{
    public sealed class UpdateVideoDetailRequest
    {
        public string VideoUrl { get; set; } = string.Empty;

        public int DurationSeconds { get; set; }

        public string? Content { get; set; }
    }
}
