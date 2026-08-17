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

        if (question.QuizMaterial.Material.Module.Course.IsPublished)
        {
            return ResponseDto<object>
                .FailResult(
                    "COURSE_NOT_MODIFIABLE",
                    "Không thể sắp xếp lại đáp án khi khóa học đang ở trạng thái công khai. Vui lòng chuyển khóa học sang trạng thái ẩn trước khi thay đổi.");
        }

        var answers = await _uow.AnswerOptions
            .GetByQuestionIdAsync(
                request.QuestionId,
                ct);

        var map = answers.ToDictionary(x => x.Id);

        await _uow.BeginTransactionAsync(ct);
        try
        {
            const int tempOffset = 1_000_000;

            // Pha 1: Đẩy OrderIndex sang dải tạm để giải phóng vị trí (tránh vi phạm unique index)
            foreach (var item in request.Items)
            {
                if (map.TryGetValue(item.AnswerOptionId, out var answer))
                {
                    answer.ChangeOrder(answer.OrderIndex + tempOffset);
                }
            }

            await _uow.SaveChangesAsync(ct);

            // Pha 2: Gán OrderIndex thực tế
            foreach (var item in request.Items)
            {
                if (map.TryGetValue(item.AnswerOptionId, out var answer))
                {
                    answer.ChangeOrder(item.NewOrderIndex);
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

        return ResponseDto<object>.SuccessResult(null!);
    }
}
