using AILA.Application.Features.VideoMaterials.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.VideoMaterials.Commands.CreateVideoMaterial;

public sealed record CreateVideoMaterialCommand(
    Guid ExpertId,
    Guid ModuleId,
    string Title,
    string VideoUrl,
    int DurationSeconds,
    string? Content
) : IRequest<ResponseDto<VideoMaterialDto>>;
