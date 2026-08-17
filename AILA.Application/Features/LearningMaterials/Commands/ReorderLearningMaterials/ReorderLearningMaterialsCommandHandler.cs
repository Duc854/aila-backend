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

        if (module.Course.IsPublished)
        {
            return ResponseDto<object>.FailResult(
                "COURSE_NOT_MODIFIABLE",
                "Không thể sắp xếp lại học liệu khi khóa học đang ở trạng thái công khai. Vui lòng chuyển khóa học sang trạng thái ẩn trước khi thay đổi.");
        }

        var materials = await _uow.Materials
            .GetByModuleIdAsync(
                request.ModuleId,
                ct);

        var materialMap = materials.ToDictionary(x => x.Id);

        await _uow.BeginTransactionAsync(ct);
        try
        {
            const int tempOffset = 1_000_000;

            // Pha 1: Đẩy OrderIndex sang dải tạm để giải phóng vị trí (tránh vi phạm unique index)
            foreach (var item in request.Items)
            {
                if (materialMap.TryGetValue(item.MaterialId, out var material))
                {
                    material.ChangeOrder(material.OrderIndex + tempOffset);
                }
            }

            await _uow.SaveChangesAsync(ct);

            // Pha 2: Gán OrderIndex thực tế
            foreach (var item in request.Items)
            {
                if (materialMap.TryGetValue(item.MaterialId, out var material))
                {
                    material.ChangeOrder(item.NewOrderIndex);
                }
            }

            await _uow.SaveChangesAsync(ct);
            await _uow.CommitTransactionAsync(ct);
        }
        catch
        {
            await _uow.RollbackTransactionAsync(ct);
            throw;
        }

        return ResponseDto<object>.SuccessResult(null!);
    }
}
