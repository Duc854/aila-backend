using System.Collections.Generic;

namespace AILA.Application.Features.LearningMaterials.Dtos;

public sealed class ReorderLearningMaterialsRequest
{
    public List<LearningMaterialOrderItem> Items { get; set; } = new();
}
