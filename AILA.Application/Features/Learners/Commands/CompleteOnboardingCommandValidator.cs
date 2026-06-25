using FluentValidation;

namespace AILA.Application.Features.Learners.Commands
{
    public class CompleteOnboardingCommandValidator : AbstractValidator<CompleteOnboardingCommand>
    {
        public CompleteOnboardingCommandValidator()
        {
            RuleFor(x => x.TagIds).NotEmpty().WithMessage("Phải chọn ít nhất 1 tag.");
            RuleFor(x => x.LearnerType).IsInEnum();
            RuleFor(x => x.KnowledgeLevel).IsInEnum();
        }
    }
}
