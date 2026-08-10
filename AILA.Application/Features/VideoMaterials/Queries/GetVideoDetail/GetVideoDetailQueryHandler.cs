using AILA.Application.Common.Interfaces;
using AILA.Application.Features.VideoMaterials.Dtos;
using AILA.Application.Features.VideoMaterials.Mapping;
using AILA.Domain.Enums;
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

        if (video != null)
        {
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

        // Chưa có VideoMaterial (mới tạo Material, chưa từng lưu chi tiết)
        // -> kiểm tra Material gốc rồi trả về DTO rỗng để FE hiển thị form nhập lần đầu
        var material = await _uow.Materials
            .GetWithModuleAndCourseAsync(
                request.MaterialId,
                ct);

        if (material == null)
        {
            return ResponseDto<VideoMaterialDto>
                .FailResult(
                    "MATERIAL_NOT_FOUND",
                    "Không tìm thấy học liệu.");
        }

        if (material.Module.Course.ExpertId != request.ExpertId)
        {
            return ResponseDto<VideoMaterialDto>
                .FailResult(
                    "FORBIDDEN",
                    "Bạn không có quyền truy cập video này.");
        }

        if (material.MaterialType != MaterialType.Video)
        {
            return ResponseDto<VideoMaterialDto>
                .FailResult(
                    "INVALID_TYPE",
                    "Học liệu này không phải Video.");
        }

        var emptyDto = new VideoMaterialDto
        {
            MaterialId = material.Id,
            Title = material.Title,
            VideoUrl = string.Empty,
            DurationSeconds = 0,
            Content = string.Empty
        };

        return ResponseDto<VideoMaterialDto>
            .SuccessResult(emptyDto);
    }
}
