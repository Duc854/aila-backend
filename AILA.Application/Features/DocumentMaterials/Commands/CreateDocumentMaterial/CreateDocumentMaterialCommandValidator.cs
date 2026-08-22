using FluentValidation;

namespace AILA.Application.Features.DocumentMaterials.Commands.CreateDocumentMaterial;

public class CreateDocumentMaterialCommandValidator : AbstractValidator<CreateDocumentMaterialCommand>
{
    public CreateDocumentMaterialCommandValidator()
    {
        RuleFor(x => x.ModuleId)
            .NotEmpty().WithMessage("Mã học phần không được để trống.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Tiêu đề không được để trống.")
            .Length(5, 255).WithMessage("Tiêu đề học liệu phải từ 5 đến 255 ký tự.");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Nội dung tài liệu không được để trống.");
    }
}
