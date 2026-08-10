using AILA.Application.Features.QuizMaterials.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.QuizMaterials.Commands.UpdateQuizDetail;

public sealed record UpdateQuizDetailCommand(
    Guid MaterialId,
    Guid ExpertId,
    int TimeLimitMinutes,
    decimal PassingScore,
    bool ShowCorrectAnswersAfterSubmission
) : IRequest<ResponseDto<QuizMaterialDto>>;
