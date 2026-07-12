using AILA.Domain.Enums;
using FluentValidation;

namespace AILA.Application.Features.QuizMaterials.Commands.BulkCreateQuiz;

public sealed class BulkCreateQuizValidator
    : AbstractValidator<BulkCreateQuizCommand>
{
    public BulkCreateQuizValidator()
    {
        RuleFor(x => x.TimeLimitMinutes)
            .GreaterThan(0);

        RuleFor(x => x.PassingScore)
            .InclusiveBetween(0, 100);

        RuleFor(x => x.Questions)
            .NotEmpty()
            .WithMessage("Quiz phải có ít nhất một câu hỏi.");

        RuleForEach(x => x.Questions)
            .ChildRules(question =>
            {
                question.RuleFor(q => q.Content)
                    .NotEmpty()
                    .MaximumLength(2000);

                question.RuleFor(q => q.QuestionType)
                    .IsInEnum();

                question.RuleFor(q => q.Answers)
                    .NotEmpty()
                    .Must(a => a.Count >= 2)
                    .WithMessage("Mỗi câu hỏi phải có ít nhất 2 đáp án.");

                question.RuleForEach(q => q.Answers)
                    .ChildRules(answer =>
                    {
                        answer.RuleFor(a => a.Content)
                            .NotEmpty()
                            .MaximumLength(1000);
                    });
            });
    }
}