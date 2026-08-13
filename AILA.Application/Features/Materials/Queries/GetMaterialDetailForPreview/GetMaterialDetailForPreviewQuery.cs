using AILA.Application.Common.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Materials.Queries.GetMaterialDetailForPreview
{
    /// <summary>
    /// Query để lấy chi tiết học liệu cho chế độ xem trước (preview).
    /// Không kiểm tra enrollment - dành cho Expert xem trước khóa học của chính mình.
    /// </summary>
    public record GetMaterialDetailForPreviewQuery(Guid CourseId, Guid MaterialId) 
        : IRequest<ResponseDto<MaterialDetailDto>>;
}
