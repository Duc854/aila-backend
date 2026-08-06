using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Features.Categories.Dtos
{
    public class CreateCategoryResponseDto
    {
        public Guid Id { get; init; }

        public string Name { get; init; } = string.Empty;

        public bool IsActive { get; init; }

        public int OrderIndex { get; init; }
    }
}
