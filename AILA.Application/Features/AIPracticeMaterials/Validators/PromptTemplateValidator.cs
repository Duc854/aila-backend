using AILA.Application.Features.AIPracticeMaterials.Commands.CreateAIPracticeMaterial;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Features.AIPracticeMaterials.Validators
{
    public sealed class PromptTemplateValidator
        : AbstractValidator<PromptTemplateDto>
    {
        public PromptTemplateValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty()
                .WithMessage("Tiêu đề Prompt Template không được để trống.")
                .MaximumLength(100)
                .WithMessage("Tiêu đề Prompt Template không được vượt quá 100 ký tự.");

            RuleFor(x => x.Content)
                .NotEmpty()
                .WithMessage("Nội dung Prompt Template không được để trống.");
        }
    }
}
