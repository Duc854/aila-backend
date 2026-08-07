using AILA.Application.Features.DocumentMaterials.Dtos;
using AILA.Domain.Entities;

namespace AILA.Application.Features.DocumentMaterials.Mapping;

internal static class DocumentMaterialMapper
{
    internal static DocumentMaterialDto MapToDto(
        DocumentMaterial entity)
    {
        return new()
        {
            MaterialId = entity.MaterialId,
            Title = entity.Material.Title,
            Content = entity.Content
        };
    }
}
