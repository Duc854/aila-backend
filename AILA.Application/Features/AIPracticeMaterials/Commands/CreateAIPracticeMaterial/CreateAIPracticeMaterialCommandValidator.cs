using AILA.Application.Features.AIPracticeMaterials.Validators;
using FluentValidation;

namespace AILA.Application.Features.AIPracticeMaterials.Commands.CreateAIPracticeMaterial
{
    public sealed class CreateAIPracticeMaterialCommandValidator
        : AbstractValidator<CreateAIPracticeMaterialCommand>
    {
        public CreateAIPracticeMaterialCommandValidator()
        {
            RuleFor(x => x.Request)
                .NotNull()
                .WithMessage("Dữ liệu yêu cầu không hợp lệ.");

            When(x => x.Request != null, () =>
            {
                RuleFor(x => x.Request)
                    .SetValidator(new CreateAIPracticeMaterialRequestValidator());
            });
        }
    }

    public sealed class CreateAIPracticeMaterialRequestValidator
        : AbstractValidator<CreateAIPracticeMaterialRequestDto>
    {
        public CreateAIPracticeMaterialRequestValidator()
        {
            RuleFor(x => x.ModuleId)
                .NotEmpty()
                .WithMessage("Module không hợp lệ.");

            RuleFor(x => x.Title)
                .NotEmpty()
                .WithMessage("Tiêu đề học liệu không được để trống.")
                .MinimumLength(5)
                .WithMessage("Tiêu đề học liệu phải có ít nhất 5 ký tự.")
                .MaximumLength(255)
                .WithMessage("Tiêu đề học liệu không được vượt quá 255 ký tự.");

            RuleFor(x => x.Scenario)
                .NotEmpty()
                .WithMessage("Mô tả kịch bản không được để trống.");

            RuleFor(x => x.AiTask)
                .NotEmpty()
                .WithMessage("Nhiệm vụ của AI không được để trống.");

            RuleFor(x => x.LearnerTask)
                .NotEmpty()
                .WithMessage("Mục tiêu của người học không được để trống.");

            RuleFor(x => x.MaxPromptAttempts)
                .GreaterThan(0)
                .WithMessage("Số lượt Prompt tối đa phải lớn hơn 0.");

            RuleFor(x => x.Difficulty)
                .IsInEnum()
                .WithMessage("Mức độ thực hành không hợp lệ.");

            RuleForEach(x => x.PromptTemplates)
                .SetValidator(new PromptTemplateValidator());

            RuleForEach(x => x.StepGuidances)
                .SetValidator(new StepGuidanceValidator());

            RuleForEach(x => x.ScoringCriteria)
                .SetValidator(new ScoringCriteriaValidator());
        }
    }
}
