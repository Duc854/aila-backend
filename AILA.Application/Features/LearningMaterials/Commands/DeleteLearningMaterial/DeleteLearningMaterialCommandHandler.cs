using AILA.Application.Common.Interfaces;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.LearningMaterials.Commands.DeleteLearningMaterial;

public sealed class DeleteLearningMaterialCommandHandler
    : IRequestHandler<
        DeleteLearningMaterialCommand,
        ResponseDto<object>>
{
    private readonly IUnitOfWork _uow;
    public DeleteLearningMaterialCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<ResponseDto<object>> Handle(
        DeleteLearningMaterialCommand request,
        CancellationToken ct)
    {
        var material = await _uow.Materials
            .GetWithModuleAndCourseAsync(
                request.MaterialId,
                ct);

        if (material == null)
        {
            return ResponseDto<object>.FailResult(
                "MATERIAL_NOT_FOUND",
                "Không tìm thấy học liệu.");
        }

        if (material.Module.Course.ExpertId != request.ExpertId)
        {
            return ResponseDto<object>.FailResult(
                "FORBIDDEN",
                "Bạn không có quyền xóa học liệu.");
        }

        var moduleId = material.ModuleId;

        _uow.Materials.Delete(material);

        await _uow.SaveChangesAsync(ct);

        var materials = await _uow.Materials
            .GetByModuleIdAsync(moduleId, ct);

        var index = 1;

        foreach (var item in materials)
        {
            item.ChangeOrder(index++);
        }

        await _uow.SaveChangesAsync(ct);

        return ResponseDto<object>.SuccessResult(null!);
    }
}