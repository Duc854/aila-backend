using AILA.Application.Features.VideoMaterials.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.VideoMaterials.Queries.GetVideoDetail;

public sealed record GetVideoDetailQuery(
    Guid MaterialId,
    Guid ExpertId
)
    : IRequest<ResponseDto<VideoMaterialDto>>;
