using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Features.Categories.Dtos
{
        public record CategoryDto(
            Guid Id,
            string Name,
            string? Description,
            int OrderIndex,
            bool IsActive
        );
    }
