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

        var hasEnrollments = await _uow.Enrollments.HasEnrollmentsForCourseAsync(question.QuizMaterial.Material.Module.CourseId, ct);
        if (question.QuizMaterial.Material.Module.Course.IsPublished || hasEnrollments)
        {
            return ResponseDto<object>
                .FailResult(
                    "COURSE_NOT_MODIFIABLE",
                    "Không thể xóa vì khóa học đã được công khai hoặc đã có học viên đăng ký.");
        }

        _uow.Questions.Delete(question);

        await _uow.SaveChangesAsync(ct);

        return ResponseDto<object>.SuccessResult(null!);
    }
}
