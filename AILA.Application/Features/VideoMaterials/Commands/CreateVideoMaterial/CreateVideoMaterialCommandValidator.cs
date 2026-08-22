using FluentValidation;

namespace AILA.Application.Features.VideoMaterials.Commands.CreateVideoMaterial;

public class CreateVideoMaterialCommandValidator : AbstractValidator<CreateVideoMaterialCommand>
{
    public CreateVideoMaterialCommandValidator()
    {
        RuleFor(x => x.ModuleId)
            .NotEmpty().WithMessage("Mã học phần không được để trống.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Tiêu đề không được để trống.")
            .Length(5, 255).WithMessage("Tiêu đề học liệu phải từ 5 đến 255 ký tự.");

        RuleFor(x => x.VideoUrl)
            .NotEmpty().WithMessage("URL video không được để trống.");

        RuleFor(x => x.DurationSeconds)
            .GreaterThanOrEqualTo(0).WithMessage("Thời lượng video không được là số âm.");
    }
}
