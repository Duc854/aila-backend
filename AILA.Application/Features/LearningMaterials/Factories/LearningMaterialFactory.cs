using AILA.Domain.Entities;
using AILA.Domain.Enums;

namespace AILA.Application.Features.LearningMaterials.Factories;

internal static class LearningMaterialFactory
{
    internal static Material Create(
        Guid moduleId,
        string title,
        MaterialType type,
        int orderIndex)
    {
        return type switch
        {
            MaterialType.Video =>
                Material.CreateVideo(
                    moduleId,
                    title,
                    orderIndex),

            MaterialType.Document =>
                Material.CreateDocument(
                    moduleId,
                    title,
                    orderIndex),

            MaterialType.Quiz =>
                Material.CreateQuiz(
                    moduleId,
                    title,
                    orderIndex),

            MaterialType.AiPractice =>
                Material.CreateAiPractice(
                    moduleId,
                    title,
                    orderIndex),

            _ =>
                throw new NotSupportedException(
                    $"MaterialType '{type}' hiện chưa được hỗ trợ.")
        };
    }
}