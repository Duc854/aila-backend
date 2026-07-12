using AILA.Application.Common.Interfaces;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Questions.Commands.ReorderQuestions;

public sealed class ReorderQuestionsCommandHandler
    : IRequestHandler<
        ReorderQuestionsCommand,
        ResponseDto<object>>
{
    private readonly IUnitOfWork _uow;

    public ReorderQuestionsCommandHandler(
        IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<ResponseDto<object>> Handle(
        ReorderQuestionsCommand request,
        CancellationToken ct)
    {
        var quiz = await _uow.Materials
            .GetQuizDetailForExpertAsync(
                request.QuizMaterialId,
                ct);

        if (quiz == null)
        {
            return ResponseDto<object>
                .FailResult(
                    "QUIZ_NOT_FOUND",
                    "Không tìm thấy Quiz.");
        }

        if (quiz.Material.Module.Course.ExpertId != request.ExpertId)
        {
            return ResponseDto<object>
                .FailResult(
                    "FORBIDDEN",
                    "Bạn không có quyền sắp xếp câu hỏi.");
        }

        var questions = await _uow.Questions
            .GetByQuizIdAsync(
                request.QuizMaterialId,
                ct);

        var map = questions.ToDictionary(x => x.Id);

        foreach (var item in request.Items)
        {
            if (map.TryGetValue(item.QuestionId, out var question))
            {
                question.ChangeOrder(item.NewOrderIndex);
            }
        }

        await _uow.SaveChangesAsync(ct);

        return ResponseDto<object>
            .SuccessResult(null!);
    }
}