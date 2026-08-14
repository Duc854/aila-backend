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

        var hasEnrollments = await _uow.Enrollments.HasEnrollmentsForCourseAsync(answer.Question.QuizMaterial.Material.Module.CourseId, ct);
        if (answer.Question.QuizMaterial.Material.Module.Course.IsPublished || hasEnrollments)
        {
            return ResponseDto<object>
                .FailResult(
                    "COURSE_NOT_MODIFIABLE",
                    "Không thể xóa đáp án vì khóa học đã được công khai hoặc đã có học viên đăng ký.");
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
