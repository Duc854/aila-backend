using AILA.Application.Features.AIPracticeMaterials.Commands.CreateAIPracticeMaterial;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Features.AIPracticeMaterials.Validators
{
    public sealed class StepGuidanceValidator
        : AbstractValidator<StepGuidanceDto>
    {
        public StepGuidanceValidator()
        {
            RuleFor(x => x.OrderIndex)
                .GreaterThan(0)
                .WithMessage("Thứ tự Step Guidance phải lớn hơn 0.");

            RuleFor(x => x.Content)
                .NotEmpty()
                .WithMessage("Nội dung Step Guidance không được để trống.");
        }
    }
}
