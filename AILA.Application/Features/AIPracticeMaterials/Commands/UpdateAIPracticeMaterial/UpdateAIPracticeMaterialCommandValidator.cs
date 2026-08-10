using AILA.Application.Features.AIPracticeMaterials.Validators;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Features.AIPracticeMaterials.Commands.UpdateAIPracticeMaterial
{
    public sealed class UpdateAIPracticeMaterialCommandValidator
    : AbstractValidator<UpdateAIPracticeMaterialCommand>
    {
        public UpdateAIPracticeMaterialCommandValidator()
        {
            RuleFor(x => x.Request)
                .NotNull()
                .WithMessage("Dữ liệu yêu cầu không hợp lệ.");

            When(x => x.Request != null, () =>
            {
                RuleFor(x => x.Request)
                    .SetValidator(new UpdateAIPracticeMaterialRequestValidator());
            });
        }
    }
    public sealed class UpdateAIPracticeMaterialRequestValidator
        : AbstractValidator<UpdateAIPracticeMaterialDto>
    {
        public UpdateAIPracticeMaterialRequestValidator()
        {
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

            RuleForEach(x => x.PromptTemplates)
                .SetValidator(new PromptTemplateValidator());

            RuleForEach(x => x.StepGuidances)
                .SetValidator(new StepGuidanceValidator());

            RuleForEach(x => x.ScoringCriteria)
                .SetValidator(new ScoringCriteriaValidator());
        }
    }
}
