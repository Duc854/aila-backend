using AILA.Application.Common.Interfaces;
using AILA.Application.Features.VideoMaterials.Dtos;
using AILA.Application.Features.VideoMaterials.Mapping;
using AILA.Domain.Entities;
using MediatR;
using Shared.Wrappers;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AILA.Application.Features.VideoMaterials.Commands.CreateVideoMaterial;

public sealed class CreateVideoMaterialCommandHandler
    : IRequestHandler<CreateVideoMaterialCommand, ResponseDto<VideoMaterialDto>>
{
    private readonly IUnitOfWork _uow;

    public CreateVideoMaterialCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<ResponseDto<VideoMaterialDto>> Handle(
        CreateVideoMaterialCommand request,
        CancellationToken ct)
    {
        var module = await _uow.Modules.GetWithCourseAsync(request.ModuleId, ct);
        if (module == null)
        {
            return ResponseDto<VideoMaterialDto>.FailResult(
                "MODULE_NOT_FOUND",
                "Không tìm thấy chương học.");
        }

        if (module.Course.ExpertId != request.ExpertId)
        {
            return ResponseDto<VideoMaterialDto>.FailResult(
                "FORBIDDEN",
                "Bạn không có quyền thêm học liệu.");
        }

        if (module.Course.IsPublished)
        {
            return ResponseDto<VideoMaterialDto>.FailResult(
                "COURSE_NOT_MODIFIABLE",
                "Không thể thêm học liệu khi khóa học đang ở trạng thái công khai.");
        }

        var nextOrderIndex = module.Materials.Any()
            ? module.Materials.Max(x => x.OrderIndex) + 1
            : 1;

        try
        {
            await _uow.BeginTransactionAsync(ct);

            var material = Material.CreateVideo(request.ModuleId, request.Title, nextOrderIndex);
            await _uow.Materials.AddAsync(material);

            var video = new VideoMaterial(material.Id, request.VideoUrl, request.DurationSeconds, request.Content);
            await _uow.Repository<VideoMaterial>().AddAsync(video);

            await _uow.CommitTransactionAsync(ct);

            return ResponseDto<VideoMaterialDto>.SuccessResult(
                VideoMaterialMapper.MapToDto(video));
        }
        catch (Exception ex)
        {
            await _uow.RollbackTransactionAsync(ct);
            return ResponseDto<VideoMaterialDto>.FailResult(
                "CREATE_FAILED",
                ex.Message);
        }
    }
}
