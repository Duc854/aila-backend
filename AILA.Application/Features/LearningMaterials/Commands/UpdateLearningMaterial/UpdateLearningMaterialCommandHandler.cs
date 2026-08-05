using AILA.Application.Common.Interfaces;
using AILA.Application.Features.LearningMaterials.Dtos;
using AILA.Application.Features.LearningMaterials.Mapping;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.LearningMaterials.Commands.UpdateLearningMaterial;

public sealed class UpdateLearningMaterialCommandHandler
    : IRequestHandler<
        UpdateLearningMaterialCommand,
        ResponseDto<LearningMaterialDto>>
{
    private readonly IUnitOfWork _uow;

    public UpdateLearningMaterialCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<ResponseDto<LearningMaterialDto>> Handle(
        UpdateLearningMaterialCommand request,
        CancellationToken ct)
    {
        var material = await _uow.Materials
            .GetWithModuleAndCourseAsync(
                request.MaterialId,
                ct);

        if (material == null)
        {
            return ResponseDto<LearningMaterialDto>
                .FailResult(
                    "MATERIAL_NOT_FOUND",
                    "Không tìm thấy học liệu.");
        }

        if (material.Module.Course.ExpertId != request.ExpertId)
        {
            return ResponseDto<LearningMaterialDto>
                .FailResult(
                    "FORBIDDEN",
                    "Bạn không có quyền chỉnh sửa học liệu.");
        }

        material.UpdateTitle(request.Title);

        await _uow.SaveChangesAsync(ct);

        return ResponseDto<LearningMaterialDto>
            .SuccessResult(
                LearningMaterialMapper.MapToDto(material));
    }
}
