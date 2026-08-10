using AILA.Application.Features.VideoMaterials.Dtos;
using MediatR;
using Shared.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Features.VideoMaterials.Commands.UpdateVideoDetail
{
    public sealed record UpdateVideoDetailCommand(
    Guid MaterialId,
    Guid ExpertId,
    string VideoUrl,
    int DurationSeconds,
    string? Content
) : IRequest<ResponseDto<VideoMaterialDto>>;
}
