using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Features.DocumentMaterials.Dtos
{
    public sealed class DocumentMaterialDto
    {
        public Guid MaterialId { get; init; }

        public string Title { get; init; } = string.Empty;

        public string Content { get; init; } = string.Empty;
    }
}
