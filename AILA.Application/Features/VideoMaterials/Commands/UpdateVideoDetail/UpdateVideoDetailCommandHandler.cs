using AILA.Application.Common.Interfaces;
using AILA.Application.Features.VideoMaterials.Dtos;
using AILA.Application.Features.VideoMaterials.Mapping;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.VideoMaterials.Commands.UpdateVideoDetail;

public sealed class UpdateVideoDetailCommandHandler
    : IRequestHandler<
        UpdateVideoDetailCommand,
        ResponseDto<VideoMaterialDto>>
{
    private readonly IUnitOfWork _uow;

    public UpdateVideoDetailCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<ResponseDto<VideoMaterialDto>> Handle(
        UpdateVideoDetailCommand request,
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
                        "Bạn không có quyền chỉnh sửa video này.");
            }

            video.UpdateDetails(
                request.VideoUrl,
                request.DurationSeconds,
                request.Content);

            await _uow.SaveChangesAsync(ct);

            return ResponseDto<VideoMaterialDto>
                .SuccessResult(
                    VideoMaterialMapper.MapToDto(video));
        }

        // Chưa có VideoMaterial -> kiểm tra Material gốc rồi tạo mới (upsert)
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
                    "Bạn không có quyền chỉnh sửa video này.");
        }

        if (material.MaterialType != MaterialType.Video)
        {
            return ResponseDto<VideoMaterialDto>
                .FailResult(
                    "INVALID_TYPE",
                    "Học liệu này không phải Video.");
        }

        var newVideo = new VideoMaterial(
            request.MaterialId,
            request.VideoUrl,
            request.DurationSeconds,
            request.Content);

        await _uow.Repository<VideoMaterial>().AddAsync(newVideo);

        await _uow.SaveChangesAsync(ct);

        return ResponseDto<VideoMaterialDto>
            .SuccessResult(
                VideoMaterialMapper.MapToDto(newVideo));
    }
}