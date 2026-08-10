using FluentValidation;

namespace AILA.Application.Features.DocumentMaterials.Commands.UpdateDocumentDetail;

public class UpdateDocumentDetailValidator
    : AbstractValidator<UpdateDocumentDetailCommand>
{
    public UpdateDocumentDetailValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty()
            .MaximumLength(50000);
    }
}
