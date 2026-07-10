using AILA.Application.Common.Interfaces;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.AnswerOptions.Commands.DeleteAnswerOption;

public sealed class DeleteAnswerOptionCommandHandler
    : IRequestHandler<
        DeleteAnswerOptionCommand,
        ResponseDto<object>>
{
    private readonly IUnitOfWork _uow;

    public DeleteAnswerOptionCommandHandler(
        IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<ResponseDto<object>> Handle(
        DeleteAnswerOptionCommand request,
        CancellationToken ct)
    {
        var answer = await _uow.AnswerOptions
            .GetWithQuestionAsync(
                request.AnswerOptionId,
                ct);

        if (answer == null)
        {
            return ResponseDto<object>
                .FailResult(
                    "ANSWER_NOT_FOUND",
                    "Không tìm thấy đáp án.");
        }

        if (answer.Question.QuizMaterial.Material.Module.Course.ExpertId
            != request.ExpertId)
        {
            return ResponseDto<object>
                .FailResult(
                    "FORBIDDEN",
                    "Bạn không có quyền xóa.");
        }

        var question = await _uow.Questions
            .GetWithQuizAndAnswersAsync(
                answer.QuestionId,
                ct);

        question!.RemoveAnswerOption(answer.Id);

        try
        {
            question.ValidateAnswerOptions();
        }
        catch (InvalidOperationException ex)
        {
            return ResponseDto<object>
                .FailResult(
                    "INVALID_ANSWER_OPTIONS",
                    ex.Message);
        }

        _uow.AnswerOptions.Delete(answer);

        await _uow.SaveChangesAsync(ct);

        return ResponseDto<object>.SuccessResult(null!);
    }
}