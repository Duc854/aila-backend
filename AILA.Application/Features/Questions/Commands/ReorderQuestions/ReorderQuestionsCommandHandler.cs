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

        await _uow.BeginTransactionAsync(ct);
        try
        {
            // Pha 1: đẩy toàn bộ OrderIndex đang bị đổi sang dải tạm
            // (không trùng với bất kỳ OrderIndex hiện có nào trong quiz này)
            // để giải phóng chỗ trước khi gán giá trị thật.
            const int tempOffset = 1_000_000;

            foreach (var item in request.Items)
            {
                if (map.TryGetValue(item.QuestionId, out var question))
                {
                    question.ChangeOrder(question.OrderIndex + tempOffset);
                }
            }

            await _uow.SaveChangesAsync(ct);

            // Pha 2: gán OrderIndex thật theo yêu cầu.
            foreach (var item in request.Items)
            {
                if (map.TryGetValue(item.QuestionId, out var question))
                {
                    question.ChangeOrder(item.NewOrderIndex);
                }
            }

            await _uow.SaveChangesAsync(ct);
            await _uow.CommitTransactionAsync(ct);
        }
        catch
        {
            await _uow.RollbackTransactionAsync(ct);
            throw;
        }

        return ResponseDto<object>
            .SuccessResult(null!);
    }
}
