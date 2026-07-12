using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Features.Categories.Dtos
{
    public class ReorderCategoriesRequest
    {
        public List<Guid> CategoryIds { get; set; } = new();
    }
}