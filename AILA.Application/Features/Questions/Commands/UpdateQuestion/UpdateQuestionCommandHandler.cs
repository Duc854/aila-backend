using AILA.Application.Common.Interfaces;
using AILA.Application.Features.Questions.Dtos;
using AILA.Application.Features.Questions.Mapping;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Questions.Commands.UpdateQuestion;

public sealed class UpdateQuestionCommandHandler
    : IRequestHandler<
        UpdateQuestionCommand,
        ResponseDto<QuestionDto>>
{
    private readonly IUnitOfWork _uow;

    public UpdateQuestionCommandHandler(
        IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<ResponseDto<QuestionDto>> Handle(
        UpdateQuestionCommand request,
        CancellationToken ct)
    {
        var question = await _uow.Questions
            .GetWithQuizAsync(
                request.QuestionId,
                ct);

        if (question == null)
        {
            return ResponseDto<QuestionDto>
                .FailResult(
                    "QUESTION_NOT_FOUND",
                    "Không tìm thấy câu hỏi.");
        }

        if (question.QuizMaterial.Material.Module.Course.ExpertId
            != request.ExpertId)
        {
            return ResponseDto<QuestionDto>
                .FailResult(
                    "FORBIDDEN",
                    "Bạn không có quyền chỉnh sửa câu hỏi.");
        }

        question.Update(
            request.Content,
            request.QuestionType,
            question.OrderIndex);

        await _uow.SaveChangesAsync(ct);

        return ResponseDto<QuestionDto>
            .SuccessResult(
                QuestionMapper.MapToDto(question));
    }
}
