using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Features.Categories.Dtos
{
    public class UpdateCategoryRequest
    {
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }
    }
}