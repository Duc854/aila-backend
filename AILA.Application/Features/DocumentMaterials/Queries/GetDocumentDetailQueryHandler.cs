using AILA.Application.Common.Interfaces;
using AILA.Application.Features.DocumentMaterials.Dtos;
using AILA.Application.Features.DocumentMaterials.Mapping;
using AILA.Domain.Enums;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.DocumentMaterials.Queries.GetDocumentDetail;

public sealed class GetDocumentDetailQueryHandler
    : IRequestHandler<GetDocumentDetailQuery,
        ResponseDto<DocumentMaterialDto>>
{
    private readonly IUnitOfWork _uow;

    public GetDocumentDetailQueryHandler(
        IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<ResponseDto<DocumentMaterialDto>> Handle(
        GetDocumentDetailQuery request,
        CancellationToken ct)
    {
        var document = await _uow.Materials
            .GetDocumentDetailForExpertAsync(
                request.MaterialId,
                ct);

        if (document != null)
        {
            if (document.Material.Module.Course.ExpertId != request.ExpertId)
            {
                return ResponseDto<DocumentMaterialDto>
                    .FailResult(
                        "FORBIDDEN",
                        "Bạn không có quyền truy cập tài liệu này.");
            }

            return ResponseDto<DocumentMaterialDto>
                .SuccessResult(
                    DocumentMaterialMapper.MapToDto(document));
        }

        var material = await _uow.Materials
            .GetWithModuleAndCourseAsync(
                request.MaterialId,
                ct);

        if (material == null)
        {
            return ResponseDto<DocumentMaterialDto>
                .FailResult(
                    "MATERIAL_NOT_FOUND",
                    "Không tìm thấy học liệu.");
        }

        if (material.Module.Course.ExpertId != request.ExpertId)
        {
            return ResponseDto<DocumentMaterialDto>
                .FailResult(
                    "FORBIDDEN",
                    "Bạn không có quyền truy cập tài liệu này.");
        }

        if (material.MaterialType != MaterialType.Document)
        {
            return ResponseDto<DocumentMaterialDto>
                .FailResult(
                    "INVALID_TYPE",
                    "Học liệu này không phải Document.");
        }

        var emptyDto = new DocumentMaterialDto
        {
            MaterialId = material.Id,
            Title = material.Title,
            Content = string.Empty
        };

        return ResponseDto<DocumentMaterialDto>
            .SuccessResult(emptyDto);
    }
}
