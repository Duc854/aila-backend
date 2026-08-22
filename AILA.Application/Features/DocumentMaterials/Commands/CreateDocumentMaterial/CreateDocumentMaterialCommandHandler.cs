using AILA.Application.Common.Interfaces;
using AILA.Application.Features.DocumentMaterials.Dtos;
using AILA.Application.Features.DocumentMaterials.Mapping;
using AILA.Domain.Entities;
using MediatR;
using Shared.Wrappers;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AILA.Application.Features.DocumentMaterials.Commands.CreateDocumentMaterial;

public sealed class CreateDocumentMaterialCommandHandler
    : IRequestHandler<CreateDocumentMaterialCommand, ResponseDto<DocumentMaterialDto>>
{
    private readonly IUnitOfWork _uow;

    public CreateDocumentMaterialCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<ResponseDto<DocumentMaterialDto>> Handle(
        CreateDocumentMaterialCommand request,
        CancellationToken ct)
    {
        var module = await _uow.Modules.GetWithCourseAsync(request.ModuleId, ct);
        if (module == null)
        {
            return ResponseDto<DocumentMaterialDto>.FailResult(
                "MODULE_NOT_FOUND",
                "Không tìm thấy chương học.");
        }

        if (module.Course.ExpertId != request.ExpertId)
        {
            return ResponseDto<DocumentMaterialDto>.FailResult(
                "FORBIDDEN",
                "Bạn không có quyền thêm học liệu.");
        }

        if (module.Course.IsPublished)
        {
            return ResponseDto<DocumentMaterialDto>.FailResult(
                "COURSE_NOT_MODIFIABLE",
                "Không thể thêm học liệu khi khóa học đang ở trạng thái công khai.");
        }

        var nextOrderIndex = module.Materials.Any()
            ? module.Materials.Max(x => x.OrderIndex) + 1
            : 1;

        try
        {
            await _uow.BeginTransactionAsync(ct);

            var material = Material.CreateDocument(request.ModuleId, request.Title, nextOrderIndex);
            await _uow.Materials.AddAsync(material);

            var document = new DocumentMaterial(material.Id, request.Content);
            await _uow.Repository<DocumentMaterial>().AddAsync(document);

            await _uow.CommitTransactionAsync(ct);

            return ResponseDto<DocumentMaterialDto>.SuccessResult(
                DocumentMaterialMapper.MapToDto(document));
        }
        catch (Exception ex)
        {
            await _uow.RollbackTransactionAsync(ct);
            return ResponseDto<DocumentMaterialDto>.FailResult(
                "CREATE_FAILED",
                ex.Message);
        }
    }
}
