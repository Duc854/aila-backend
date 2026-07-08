using AILA.Application.Common.Interfaces;
using AILA.Application.Features.LearningMaterials.Dtos;
using AILA.Application.Features.LearningMaterials.Mapping;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.LearningMaterials.Queries.GetLearningMaterialsByModule;

public sealed class GetLearningMaterialsByModuleQueryHandler
    : IRequestHandler<
        GetLearningMaterialsByModuleQuery,
        ResponseDto<List<LearningMaterialDto>>>
{
    private readonly IUnitOfWork _uow;

    public GetLearningMaterialsByModuleQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<ResponseDto<List<LearningMaterialDto>>> Handle(
        GetLearningMaterialsByModuleQuery request,
        CancellationToken cancellationToken)
    {
        var module = await _uow.Modules.GetWithCourseAsync(
            request.ModuleId,
            cancellationToken);

        if (module == null)
        {
            return ResponseDto<List<LearningMaterialDto>>
                .FailResult(
                    "MODULE_NOT_FOUND",
                    "Không tìm thấy chương học.");
        }

        if (module.Course.ExpertId != request.ExpertId)
        {
            return ResponseDto<List<LearningMaterialDto>>
                .FailResult(
                    "FORBIDDEN",
                    "Bạn không có quyền xem học liệu của chương học này.");
        }

        var materials = module.Materials
            .OrderBy(m => m.OrderIndex)
            .Select(LearningMaterialMapper.MapToDto)
            .ToList();

        return ResponseDto<List<LearningMaterialDto>>
            .SuccessResult(materials);
    }
}