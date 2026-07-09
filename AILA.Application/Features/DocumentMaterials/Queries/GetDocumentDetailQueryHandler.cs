using AILA.Application.Common.Interfaces;
using AILA.Application.Features.DocumentMaterials.Dtos;
using AILA.Application.Features.DocumentMaterials.Mapping;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.DocumentMaterials.Queries.GetDocumentDetail;

public sealed class GetDocumentDetailQueryHandler
    : IRequestHandler<GetDocumentDetailQuery,
        ResponseDto<DocumentMaterialDto>>
{
    private readonly IUnitOfWork _uow;

    public GetDocumentDetailQueryHandler(IUnitOfWork uow)
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

        if (document == null)
        {
            return ResponseDto<DocumentMaterialDto>
                .FailResult(
                    "DOCUMENT_NOT_FOUND",
                    "Không tìm thấy tài liệu.");
        }

        if (document.Material.Module.Course.ExpertId != request.ExpertId)
        {
            return ResponseDto<DocumentMaterialDto>
                .FailResult(
                    "FORBIDDEN",
                    "Bạn không có quyền truy cập.");
        }

        return ResponseDto<DocumentMaterialDto>
            .SuccessResult(
                DocumentMaterialMapper.MapToDto(document));
    }
}