using AILA.Application.Common.Interfaces;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.LearningMaterials.Commands.ReorderLearningMaterials;

public sealed class ReorderLearningMaterialsCommandHandler
    : IRequestHandler<
        ReorderLearningMaterialsCommand,
        ResponseDto<object>>
{
    private readonly IUnitOfWork _uow;

    public ReorderLearningMaterialsCommandHandler(
        IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<ResponseDto<object>> Handle(
        ReorderLearningMaterialsCommand request,
        CancellationToken ct)
    {
        var module = await _uow.Modules
            .GetWithCourseAsync(
                request.ModuleId,
                ct);

        if (module == null)
        {
            return ResponseDto<object>.FailResult(
                "MODULE_NOT_FOUND",
                "Không tìm thấy chương học.");
        }

        if (module.Course.ExpertId != request.ExpertId)
        {
            return ResponseDto<object>.FailResult(
                "FORBIDDEN",
                "Bạn không có quyền sắp xếp học liệu.");
        }

        var materials = await _uow.Materials
            .GetByModuleIdAsync(
                request.ModuleId,
                ct);

        var materialMap = materials.ToDictionary(x => x.Id);

        foreach (var item in request.Items)
        {
            if (materialMap.TryGetValue(item.MaterialId, out var material))
            {
                material.ChangeOrder(item.NewOrderIndex);
            }
        }

        await _uow.SaveChangesAsync(ct);

        return ResponseDto<object>.SuccessResult(null!);
    }
}
