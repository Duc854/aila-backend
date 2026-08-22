using FluentValidation;

namespace AILA.Application.Features.QuizMaterials.Commands.CreateQuizMaterial;

public class CreateQuizMaterialCommandValidator : AbstractValidator<CreateQuizMaterialCommand>
{
    public CreateQuizMaterialCommandValidator()
    {
        RuleFor(x => x.ModuleId)
            .NotEmpty().WithMessage("Mã học phần không được để trống.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Tiêu đề không được để trống.")
            .Length(5, 255).WithMessage("Tiêu đề học liệu phải từ 5 đến 255 ký tự.");

        RuleFor(x => x.TimeLimitMinutes)
            .GreaterThan(0).WithMessage("Thời gian làm bài phải lớn hơn 0 phút.");

        RuleFor(x => x.PassingScore)
            .InclusiveBetween(0, 100).WithMessage("Điểm đạt phải nằm trong khoảng từ 0 đến 100.");
    }
}
