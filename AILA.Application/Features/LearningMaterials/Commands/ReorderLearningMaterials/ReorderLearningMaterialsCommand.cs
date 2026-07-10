using AILA.Application.Features.LearningMaterials.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.LearningMaterials.Commands.ReorderLearningMaterials;

public sealed record ReorderLearningMaterialsCommand(
    Guid ModuleId,
    Guid ExpertId,
    List<LearningMaterialOrderItem> Items
) : IRequest<ResponseDto<object>>;