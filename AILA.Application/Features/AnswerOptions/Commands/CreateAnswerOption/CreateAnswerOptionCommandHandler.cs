using AILA.Application.Common.Interfaces;
using AILA.Application.Features.AnswerOptions.Dtos;
using AILA.Application.Features.AnswerOptions.Mapping;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.AnswerOptions.Commands.CreateAnswerOption;

public sealed class CreateAnswerOptionCommandHandler
    : IRequestHandler<
        CreateAnswerOptionCommand,
        ResponseDto<AnswerOptionDto>>
{
    private readonly IUnitOfWork _uow;

    public CreateAnswerOptionCommandHandler(
        IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<ResponseDto<AnswerOptionDto>> Handle(
        CreateAnswerOptionCommand request,
        CancellationToken ct)
    {
        var question = await _uow.Questions
            .GetWithQuizAndAnswersAsync(
                request.QuestionId,
                ct);

        if (question == null)
        {
            return ResponseDto<AnswerOptionDto>
                .FailResult(
                    "QUESTION_NOT_FOUND",
                    "Không tìm thấy câu hỏi.");
        }

        if (question.QuizMaterial.Material.Module.Course.ExpertId
            != request.ExpertId)
        {
            return ResponseDto<AnswerOptionDto>
                .FailResult(
                    "FORBIDDEN",
                    "Bạn không có quyền chỉnh sửa.");
        }

        if (question.QuizMaterial.Material.Module.Course.IsPublished)
        {
            return ResponseDto<AnswerOptionDto>
                .FailResult(
                    "COURSE_NOT_MODIFIABLE",
                    "Không thể thêm đáp án khi khóa học đang ở trạng thái công khai. Vui lòng chuyển khóa học sang trạng thái ẩn trước khi thay đổi.");
        }

        var nextOrder =
            question.AnswerOptions.Any()
                ? question.AnswerOptions.Max(x => x.OrderIndex) + 1
                : 1;

        var answer = new AnswerOption(
            request.QuestionId,
            request.Content,
            request.IsCorrect,
            nextOrder);

        question.AddAnswerOption(answer);

        try
        {
            question.ValidateAnswerOptions();
        }
        catch (InvalidOperationException ex)
        {
            return ResponseDto<AnswerOptionDto>
                .FailResult(
                    "INVALID_ANSWER_OPTIONS",
                    ex.Message);
        }

        await _uow.AnswerOptions.AddAsync(answer);

        await _uow.SaveChangesAsync(ct);

        return ResponseDto<AnswerOptionDto>
            .SuccessResult(
                AnswerOptionMapper.MapToDto(answer));
    }
}
