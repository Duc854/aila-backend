using AILA.Domain.Enums;
using FluentValidation;

namespace AILA.Application.Features.LearningMaterials.Commands.CreateLearningMaterial;

public class CreateLearningMaterialValidator
    : AbstractValidator<CreateLearningMaterialCommand>
{
    public CreateLearningMaterialValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MinimumLength(5)
            .MaximumLength(255);

        RuleFor(x => x.MaterialType)
            .Must(x =>
                x == MaterialType.Video ||
                x == MaterialType.Document ||
                x == MaterialType.Quiz ||
                x == MaterialType.AiPractice)
            .WithMessage(
                "Chỉ hỗ trợ Video, Document, Quiz và AiPractice.");
    }
}