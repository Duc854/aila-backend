using AILA.Domain.Enums;

namespace AILA.Application.Features.LearningMaterials.Dtos;

public class SaveLearningMaterialRequest
{
    public string Title { get; set; } = string.Empty;

    public MaterialType MaterialType { get; set; }
}
