using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Features.LearningMaterials.Dtos
{
    public sealed class LearningMaterialOrderItem
    {
        public Guid MaterialId { get; init; }

        public int NewOrderIndex { get; init; }
    }
}
