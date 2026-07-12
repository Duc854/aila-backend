using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Features.Categories.Dtos
{
    public class CreateCategoryRequest
    {
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public int OrderIndex { get; set; }
    }
}