using AILA.Application.Common.Interfaces;
using AILA.Application.Features.AnswerOptions.Dtos;
using AILA.Application.Features.AnswerOptions.Mapping;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.AnswerOptions.Commands.UpdateAnswerOption;

public sealed class UpdateAnswerOptionCommandHandler
    : IRequestHandler<
        UpdateAnswerOptionCommand,
        ResponseDto<AnswerOptionDto>>
{
    private readonly IUnitOfWork _uow;

    public UpdateAnswerOptionCommandHandler(
        IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<ResponseDto<AnswerOptionDto>> Handle(
        UpdateAnswerOptionCommand request,
        CancellationToken ct)
    {
        var answer = await _uow.AnswerOptions
            .GetWithQuestionAsync(
                request.AnswerOptionId,
                ct);

        if (answer == null)
        {
            return ResponseDto<AnswerOptionDto>
                .FailResult(
                    "ANSWER_NOT_FOUND",
                    "Không tìm thấy đáp án.");
        }

        if (answer.Question.QuizMaterial.Material.Module.Course.ExpertId
            != request.ExpertId)
        {
            return ResponseDto<AnswerOptionDto>
                .FailResult(
                    "FORBIDDEN",
                    "Bạn không có quyền chỉnh sửa.");
        }

        answer.Update(
            request.Content,
            request.IsCorrect,
            answer.OrderIndex);

        var question = await _uow.Questions
            .GetWithQuizAndAnswersAsync(
                answer.QuestionId,
                ct);

        try
        {
            question!.ValidateAnswerOptions();
        }
        catch (InvalidOperationException ex)
        {
            return ResponseDto<AnswerOptionDto>
                .FailResult(
                    "INVALID_ANSWER_OPTIONS",
                    ex.Message);
        }

        await _uow.SaveChangesAsync(ct);

        return ResponseDto<AnswerOptionDto>
            .SuccessResult(
                AnswerOptionMapper.MapToDto(answer));
    }
}