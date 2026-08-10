using AILA.Application.Common.Interfaces;
using AILA.Application.Features.AnswerOptions.Dtos;
using AILA.Application.Features.AnswerOptions.Mapping;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.AnswerOptions.Queries.GetAnswerOptions;

public sealed class GetAnswerOptionsQueryHandler
    : IRequestHandler<
        GetAnswerOptionsQuery,
        ResponseDto<List<AnswerOptionDto>>>
{
    private readonly IUnitOfWork _uow;

    public GetAnswerOptionsQueryHandler(
        IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<ResponseDto<List<AnswerOptionDto>>> Handle(
        GetAnswerOptionsQuery request,
        CancellationToken ct)
    {
        // Kiểm tra Question
        var question = await _uow.Questions
            .GetWithQuizAsync(
                request.QuestionId,
                ct);

        if (question == null)
        {
            return ResponseDto<List<AnswerOptionDto>>
                .FailResult(
                    "QUESTION_NOT_FOUND",
                    "Không tìm thấy câu hỏi.");
        }

        // Kiểm tra quyền Expert (bỏ qua khi admin xem preview)
        if (!request.IsAdminOverride && question.QuizMaterial.Material.Module.Course.ExpertId != request.ExpertId)
        {
            return ResponseDto<List<AnswerOptionDto>>
                .FailResult(
                    "FORBIDDEN",
                    "Bạn không có quyền truy cập.");
        }

        var answers = await _uow.AnswerOptions
            .GetByQuestionIdAsync(
                request.QuestionId,
                ct);

        var result = answers
            .Select(AnswerOptionMapper.MapToDto)
            .ToList();

        return ResponseDto<List<AnswerOptionDto>>
            .SuccessResult(result);
    }
}
