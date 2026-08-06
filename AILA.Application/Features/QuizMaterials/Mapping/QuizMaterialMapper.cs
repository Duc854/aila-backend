using AILA.Application.Features.QuizMaterials.Dtos;
using AILA.Domain.Entities;

namespace AILA.Application.Features.QuizMaterials.Mapping;

internal static class QuizMaterialMapper
{
    internal static QuizMaterialDto MapToDto(
        QuizMaterial entity)
    {
        return new QuizMaterialDto
        {
            MaterialId = entity.MaterialId,

            Title = entity.Material.Title,

            TimeLimitMinutes = entity.TimeLimitMinutes,

            PassingScore = entity.PassingScore,

            ShowCorrectAnswersAfterSubmission =
                entity.ShowCorrectAnswersAfterSubmission
        };
    }
}
