using AILA.Application.Features.QuizMaterials.Dtos.BulkCreateQuiz;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.QuizMaterials.Commands.BulkCreateQuiz;

public sealed record BulkCreateQuizCommand(
    Guid MaterialId,
    Guid ExpertId,
    int TimeLimitMinutes,
    decimal PassingScore,
    bool ShowCorrectAnswersAfterSubmission,
    List<BulkQuestionDto> Questions
) : IRequest<ResponseDto<object>>;
