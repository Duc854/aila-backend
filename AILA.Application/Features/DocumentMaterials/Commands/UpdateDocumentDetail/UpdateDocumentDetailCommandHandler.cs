using AILA.Application.Common.Interfaces;
using AILA.Application.Features.DocumentMaterials.Dtos;
using AILA.Application.Features.DocumentMaterials.Mapping;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.DocumentMaterials.Commands.UpdateDocumentDetail;

public sealed class UpdateDocumentDetailCommandHandler
    : IRequestHandler<UpdateDocumentDetailCommand,
        ResponseDto<DocumentMaterialDto>>
{
    private readonly IUnitOfWork _uow;

    public UpdateDocumentDetailCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<ResponseDto<DocumentMaterialDto>> Handle(
        UpdateDocumentDetailCommand request,
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
                    "Bạn không có quyền chỉnh sửa.");
        }

        document.UpdateDetails(request.Content);

        await _uow.SaveChangesAsync(ct);

        return ResponseDto<DocumentMaterialDto>
            .SuccessResult(
                DocumentMaterialMapper.MapToDto(document));
    }
}