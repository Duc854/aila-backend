using FluentValidation;

namespace AILA.Application.Features.LearningMaterials.Commands.UpdateLearningMaterial;

public class UpdateLearningMaterialValidator
    : AbstractValidator<UpdateLearningMaterialCommand>
{
    public UpdateLearningMaterialValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MinimumLength(5)
            .MaximumLength(255);
    }
}