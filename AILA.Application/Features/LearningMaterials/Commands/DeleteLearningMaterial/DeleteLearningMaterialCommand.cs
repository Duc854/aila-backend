using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.LearningMaterials.Commands.DeleteLearningMaterial;

public sealed record DeleteLearningMaterialCommand(
    Guid MaterialId,
    Guid ExpertId
) : IRequest<ResponseDto<object>>;
