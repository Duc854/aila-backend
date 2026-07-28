using AILA.Application.Features.LearningMaterials.Dtos;
using AILA.Domain.Entities;
using AILA.Domain.Enums;

namespace AILA.Application.Features.LearningMaterials.Mapping;

internal static class LearningMaterialMapper
{
    internal static LearningMaterialDto MapToDto(Material material)
    {
        return new LearningMaterialDto
        {
            Id = material.Id,
            ModuleId = material.ModuleId,
            Title = material.Title,
            MaterialType = material.MaterialType,
            MaterialTypeName = material.MaterialType switch
            {
                MaterialType.Video => "Video",
                MaterialType.Document => "Tài liệu",
                MaterialType.Quiz => "Bài kiểm tra",
                MaterialType.AiPractice => "Thực hành AI",
                _ => material.MaterialType.ToString()
            },
            OrderIndex = material.OrderIndex
        };
    }
}