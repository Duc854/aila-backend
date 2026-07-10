using AILA.Application.Features.LearningMaterials.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.LearningMaterials.Commands.UpdateLearningMaterial;

public sealed record UpdateLearningMaterialCommand(
    Guid MaterialId,
    Guid ExpertId,
    string Title
) : IRequest<ResponseDto<LearningMaterialDto>>;