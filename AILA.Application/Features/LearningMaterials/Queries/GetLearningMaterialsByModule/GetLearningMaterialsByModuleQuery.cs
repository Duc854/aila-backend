using AILA.Application.Features.LearningMaterials.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.LearningMaterials.Queries.GetLearningMaterialsByModule;

public record GetLearningMaterialsByModuleQuery(
    Guid ModuleId,
    Guid ExpertId
) : IRequest<ResponseDto<List<LearningMaterialDto>>>;