using AILA.Application.Features.AIPracticeMaterials.Commands.CreateAIPracticeMaterial;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Features.AIPracticeMaterials.Validators
{
    public sealed class ScoringCriteriaValidator
         : AbstractValidator<ScoringCriteriaDto>
    {
        public ScoringCriteriaValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty()
                .WithMessage("Tên tiêu chí chấm điểm không được để trống.");

            RuleFor(x => x.Weight)
                .GreaterThan(0)
                .WithMessage("Trọng số phải lớn hơn 0.")
                .LessThanOrEqualTo(100)
                .WithMessage("Trọng số không được vượt quá 100.");
        }
    }
}
