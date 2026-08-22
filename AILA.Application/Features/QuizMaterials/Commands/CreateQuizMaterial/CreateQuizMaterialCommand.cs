using AILA.Application.Features.QuizMaterials.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.QuizMaterials.Commands.CreateQuizMaterial;

public sealed record CreateQuizMaterialCommand(
    Guid ExpertId,
    Guid ModuleId,
    string Title,
    int TimeLimitMinutes,
    decimal PassingScore,
    bool ShowCorrectAnswersAfterSubmission
) : IRequest<ResponseDto<QuizMaterialDto>>;
