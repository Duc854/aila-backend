using AILA.Application.Features.VideoMaterials.Dtos;
using AILA.Domain.Entities;

namespace AILA.Application.Features.VideoMaterials.Mapping;

internal static class VideoMaterialMapper
{
    internal static VideoMaterialDto MapToDto(
        VideoMaterial entity)
    {
        return new VideoMaterialDto
        {
            MaterialId = entity.MaterialId,
            Title = entity.Material.Title,
            VideoUrl = entity.VideoUrl,
            DurationSeconds = entity.DurationSeconds,
            Content = entity.Content
        };
    }
}
