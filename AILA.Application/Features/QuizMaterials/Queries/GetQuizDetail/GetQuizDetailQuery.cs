using AILA.Application.Features.QuizMaterials.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.QuizMaterials.Queries.GetQuizDetail;

public sealed record GetQuizDetailQuery(
    Guid MaterialId,
    Guid ExpertId,
    bool IsAdminOverride = false)
    : IRequest<ResponseDto<QuizMaterialDto>>;
