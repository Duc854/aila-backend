using FluentValidation;

namespace AILA.Application.Features.ExpertEvaluations.Commands.RequestExpertEvaluation
{
    public sealed class RequestExpertEvaluationCommandValidator
        : AbstractValidator<RequestExpertEvaluationCommand>
    {
        public RequestExpertEvaluationCommandValidator()
        {
            RuleFor(x => x.PracticeAttemptId)
                .NotEmpty()
                .WithMessage("Mã lượt thực hành không hợp lệ.");

            RuleFor(x => x.LearnerId)
                .NotEmpty()
                .WithMessage("Mã học viên không hợp lệ.");
        }
    }
}
