using AILA.Application.Common.Interfaces;
using AILA.Application.Features.Questions.Dtos;
using AILA.Application.Features.Questions.Mapping;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Questions.Queries.GetQuestions;

public sealed class GetQuestionsQueryHandler
    : IRequestHandler<
        GetQuestionsQuery,
        ResponseDto<List<QuestionDto>>>
{
    private readonly IUnitOfWork _uow;

    public GetQuestionsQueryHandler(
        IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<ResponseDto<List<QuestionDto>>> Handle(
        GetQuestionsQuery request,
        CancellationToken ct)
    {
        // Kiểm tra Quiz có tồn tại và thuộc Expert
        var quiz = await _uow.Materials
            .GetQuizDetailForExpertAsync(
                request.QuizMaterialId,
                ct);

        if (quiz == null)
        {
            return ResponseDto<List<QuestionDto>>
                .FailResult(
                    "QUIZ_NOT_FOUND",
                    "Không tìm thấy Quiz.");
        }

        if (!request.IsAdminOverride && quiz.Material.Module.Course.ExpertId != request.ExpertId)
        {
            return ResponseDto<List<QuestionDto>>
                .FailResult(
                    "FORBIDDEN",
                    "Bạn không có quyền truy cập Quiz này.");
        }

        var questions = await _uow.Questions
            .GetByQuizIdAsync(
                request.QuizMaterialId,
                ct);

        var result = questions
            .Select(QuestionMapper.MapToDto)
            .ToList();

        return ResponseDto<List<QuestionDto>>
            .SuccessResult(result);
    }
}
