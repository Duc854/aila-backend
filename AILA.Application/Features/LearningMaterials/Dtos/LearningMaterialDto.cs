using AILA.Domain.Enums;

namespace AILA.Application.Features.LearningMaterials.Dtos;

public sealed class LearningMaterialDto
{
    public Guid Id { get; init; }

    public Guid ModuleId { get; init; }

    public string Title { get; init; } = string.Empty;

    public MaterialType MaterialType { get; init; }

    public string MaterialTypeName { get; init; } = string.Empty;

    public int OrderIndex { get; init; }
}