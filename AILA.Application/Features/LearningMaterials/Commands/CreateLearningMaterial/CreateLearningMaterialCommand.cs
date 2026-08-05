using AILA.Application.Features.LearningMaterials.Dtos;
using AILA.Domain.Enums;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.LearningMaterials.Commands.CreateLearningMaterial;

public sealed record CreateLearningMaterialCommand(
    Guid ModuleId,
    Guid ExpertId,
    string Title,
    MaterialType MaterialType
) : IRequest<ResponseDto<LearningMaterialDto>>;
