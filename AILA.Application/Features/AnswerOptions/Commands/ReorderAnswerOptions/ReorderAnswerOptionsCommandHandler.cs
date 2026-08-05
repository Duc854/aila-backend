using AILA.Application.Common.Interfaces;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.AnswerOptions.Commands.ReorderAnswerOptions;

public sealed class ReorderAnswerOptionsCommandHandler
    : IRequestHandler<
        ReorderAnswerOptionsCommand,
        ResponseDto<object>>
{
    private readonly IUnitOfWork _uow;

    public ReorderAnswerOptionsCommandHandler(
        IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<ResponseDto<object>> Handle(
        ReorderAnswerOptionsCommand request,
        CancellationToken ct)
    {
        var question = await _uow.Questions
            .GetWithQuizAndAnswersAsync(
                request.QuestionId,
                ct);

        if (question == null)
        {
            return ResponseDto<object>
                .FailResult(
                    "QUESTION_NOT_FOUND",
                    "Không tìm thấy câu hỏi.");
        }

        if (question.QuizMaterial.Material.Module.Course.ExpertId
            != request.ExpertId)
        {
            return ResponseDto<object>
                .FailResult(
                    "FORBIDDEN",
                    "Bạn không có quyền thực hiện.");
        }

        var answers = await _uow.AnswerOptions
            .GetByQuestionIdAsync(
                request.QuestionId,
                ct);

        var map = answers.ToDictionary(x => x.Id);

        foreach (var item in request.Items)
        {
            if (map.TryGetValue(item.AnswerOptionId, out var answer))
            {
                answer.ChangeOrder(item.NewOrderIndex);
            }
        }

        await _uow.SaveChangesAsync(ct);

        return ResponseDto<object>.SuccessResult(null!);
    }
}
