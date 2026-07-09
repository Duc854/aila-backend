using AILA.Application.Common.Interfaces;
using AILA.Application.Features.VideoMaterials.Dtos;
using AILA.Application.Features.VideoMaterials.Mapping;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.VideoMaterials.Queries.GetVideoDetail;

public sealed class GetVideoDetailQueryHandler
    : IRequestHandler<
        GetVideoDetailQuery,
        ResponseDto<VideoMaterialDto>>
{
    private readonly IUnitOfWork _uow;

    public GetVideoDetailQueryHandler(
        IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<ResponseDto<VideoMaterialDto>> Handle(
        GetVideoDetailQuery request,
        CancellationToken ct)
    {
        var video = await _uow.Materials
            .GetVideoDetailForExpertAsync(
                request.MaterialId,
                ct);

        if (video == null)
        {
            return ResponseDto<VideoMaterialDto>
                .FailResult(
                    "VIDEO_NOT_FOUND",
                    "Không tìm thấy video.");
        }

        if (video.Material.Module.Course.ExpertId != request.ExpertId)
        {
            return ResponseDto<VideoMaterialDto>
                .FailResult(
                    "FORBIDDEN",
                    "Bạn không có quyền truy cập video này.");
        }

        return ResponseDto<VideoMaterialDto>
            .SuccessResult(
                VideoMaterialMapper.MapToDto(video));
    }
}