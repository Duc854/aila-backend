using AILA.Application.Common.Interfaces;
using AILA.Application.Features.DocumentMaterials.Dtos;
using AILA.Application.Features.DocumentMaterials.Mapping;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
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

        if (document != null)
        {
            if (document.Material.Module.Course.ExpertId != request.ExpertId)
            {
                return ResponseDto<DocumentMaterialDto>
                    .FailResult(
                        "FORBIDDEN",
                        "Bạn không có quyền chỉnh sửa.");
            }

            var hasEnrollmentsForDoc = await _uow.Enrollments.HasEnrollmentsForCourseAsync(document.Material.Module.CourseId, ct);
            if (document.Material.Module.Course.IsPublished || hasEnrollmentsForDoc)
            {
                return ResponseDto<DocumentMaterialDto>
                    .FailResult(
                        "COURSE_NOT_MODIFIABLE",
                        "Không thể chỉnh sửa tài liệu vì khóa học đã được công khai hoặc đã có học viên đăng ký.");
            }

            document.UpdateDetails(request.Content);

            await _uow.SaveChangesAsync(ct);

            return ResponseDto<DocumentMaterialDto>
                .SuccessResult(
                    DocumentMaterialMapper.MapToDto(document));
        }

        // Chưa có DocumentMaterial -> kiểm tra Material gốc rồi tạo mới (upsert)
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
                    "Bạn không có quyền chỉnh sửa.");
        }

        var hasEnrollments = await _uow.Enrollments.HasEnrollmentsForCourseAsync(material.Module.CourseId, ct);
        if (material.Module.Course.IsPublished || hasEnrollments)
        {
            return ResponseDto<DocumentMaterialDto>
                .FailResult(
                    "COURSE_NOT_MODIFIABLE",
                    "Không thể chỉnh sửa tài liệu vì khóa học đã được công khai hoặc đã có học viên đăng ký.");
        }

        if (material.MaterialType != MaterialType.Document)
        {
            return ResponseDto<DocumentMaterialDto>
                .FailResult(
                    "INVALID_TYPE",
                    "Học liệu này không phải Document.");
        }

        var newDocument = new DocumentMaterial(
            request.MaterialId,
            request.Content);

        await _uow.Repository<DocumentMaterial>().AddAsync(newDocument);

        await _uow.SaveChangesAsync(ct);

        return ResponseDto<DocumentMaterialDto>
            .SuccessResult(
                DocumentMaterialMapper.MapToDto(newDocument));
    }
}
