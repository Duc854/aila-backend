using AILA.Application.Common.Dtos;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Features.ResourceLimitPolicy.Commands.UpdateDefaultResourceLimitPolicies
{
    public sealed class UpdateDefaultResourceLimitPoliciesCommandValidator
        : AbstractValidator<UpdateDefaultResourceLimitPoliciesCommand>
    {
        public UpdateDefaultResourceLimitPoliciesCommandValidator()
        {
            RuleFor(x => x.Policies)
                .NotEmpty()
                .WithMessage("Danh sách chính sách giới hạn tài nguyên không được để trống.");

            RuleForEach(x => x.Policies)
                .SetValidator(new ResourceLimitPolicyItemValidator());
        }
    }


    public sealed class ResourceLimitPolicyItemValidator
        : AbstractValidator<ResourceLimitPolicyDto>
    {
        public ResourceLimitPolicyItemValidator()
        {
            RuleFor(x => x.AccountType)
                .IsInEnum()
                .WithMessage("Loại tài khoản không hợp lệ.");

            RuleFor(x => x.AiTokenLimit)
                .GreaterThanOrEqualTo(0)
                .WithMessage(
                    "Giới hạn AI Token phải lớn hơn hoặc bằng 0.");

            RuleFor(x => x.AiPracticeScenarioLimit)
                .GreaterThanOrEqualTo(0)
                .WithMessage(
                    "Giới hạn lượt thực hành AI phải lớn hơn hoặc bằng 0.");

            RuleFor(x => x.ExpertEvaluationRequestLimit)
                .GreaterThanOrEqualTo(0)
                .WithMessage(
                    "Giới hạn yêu cầu đánh giá chuyên gia phải lớn hơn hoặc bằng 0.");
        }
    }
}
