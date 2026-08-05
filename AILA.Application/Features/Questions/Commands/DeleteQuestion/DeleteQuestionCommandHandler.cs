using AILA.Application.Common.Interfaces;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Questions.Commands.DeleteQuestion;

public sealed class DeleteQuestionCommandHandler
    : IRequestHandler<
        DeleteQuestionCommand,
        ResponseDto<object>>
{
    private readonly IUnitOfWork _uow;

    public DeleteQuestionCommandHandler(
        IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<ResponseDto<object>> Handle(
        DeleteQuestionCommand request,
        CancellationToken ct)
    {
        var question = await _uow.Questions
            .GetWithQuizAsync(
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
                    "Bạn không có quyền xóa câu hỏi.");
        }

        _uow.Questions.Delete(question);

        await _uow.SaveChangesAsync(ct);

        return ResponseDto<object>.SuccessResult(null!);
    }
}
