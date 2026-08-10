using AILA.Application.Common.Interfaces;
using AILA.Application.Features.LearningMaterials.Dtos;
using AILA.Application.Features.LearningMaterials.Factories;
using AILA.Application.Features.LearningMaterials.Mapping;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.LearningMaterials.Commands.CreateLearningMaterial;

public sealed class CreateLearningMaterialCommandHandler
    : IRequestHandler<CreateLearningMaterialCommand,
        ResponseDto<LearningMaterialDto>>
{
    private readonly IUnitOfWork _uow;

    public CreateLearningMaterialCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<ResponseDto<LearningMaterialDto>> Handle(
        CreateLearningMaterialCommand request,
        CancellationToken ct)
    {
        // 1. Kiểm tra Module
        var module = await _uow.Modules.GetWithCourseAsync(
            request.ModuleId,
            ct);

        if (module == null)
        {
            return ResponseDto<LearningMaterialDto>
                .FailResult(
                    "MODULE_NOT_FOUND",
                    "Không tìm thấy chương học.");
        }

        // 2. Kiểm tra quyền Expert
        if (module.Course.ExpertId != request.ExpertId)
        {
            return ResponseDto<LearningMaterialDto>
                .FailResult(
                    "FORBIDDEN",
                    "Bạn không có quyền thêm học liệu.");
        }

        // 3. Tự tính OrderIndex
        var nextOrderIndex =
            module.Materials.Any()
                ? module.Materials.Max(x => x.OrderIndex) + 1
                : 1;

        // 4. Tạo Material
        var material =
            LearningMaterialFactory.Create(
                request.ModuleId,
                request.Title,
                request.MaterialType,
                nextOrderIndex);

        // 5. Lưu
        await _uow.Materials.AddAsync(material);

        await _uow.SaveChangesAsync(ct);

        // 6. Trả DTO
        return ResponseDto<LearningMaterialDto>
            .SuccessResult(
                LearningMaterialMapper.MapToDto(material));
    }
}
