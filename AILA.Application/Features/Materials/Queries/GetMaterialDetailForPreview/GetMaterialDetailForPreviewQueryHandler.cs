using AILA.Application.Common.Dtos;
using AILA.Application.Common.Interfaces;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Materials.Queries.GetMaterialDetailForPreview
{
    /// <summary>
    /// Handler cho preview query - lấy chi tiết học liệu mà không kiểm tra enrollment.
    /// Dùng cho Expert xem trước khóa học của chính mình.
    /// </summary>
    public class GetMaterialDetailForPreviewQueryHandler 
        : IRequestHandler<GetMaterialDetailForPreviewQuery, ResponseDto<MaterialDetailDto>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetMaterialDetailForPreviewQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ResponseDto<MaterialDetailDto>> Handle(
            GetMaterialDetailForPreviewQuery request, 
            CancellationToken cancellationToken)
        {
            // Lấy chi tiết học liệu trực tiếp (bypass enrollment check)
            var material = await _unitOfWork.Materials.GetMaterialDetailAsync(
                request.CourseId, 
                request.MaterialId);

            if (material == null)
            {
                return ResponseDto<MaterialDetailDto>.FailResult(
                    "MATERIAL_NOT_FOUND", 
                    "Không tìm thấy học liệu yêu cầu trong khóa học này.");
            }

            // Map Entity sang DTO
            var dto = new MaterialDetailDto
            {
                Id = material.Id,
                ModuleId = material.ModuleId,
                Title = material.Title,
                Type = material.MaterialType.ToString(),
                OrderIndex = material.OrderIndex,
                VideoDetails = material.VideoDetails != null ? new VideoMaterialDto
                {
                    VideoUrl = material.VideoDetails.VideoUrl,
                    Content = material.VideoDetails.Content
                } : null,
                DocumentDetails = material.DocumentDetails != null ? new DocumentMaterialDto
                {
                    Content = material.DocumentDetails.Content,
                } : null
            };

            return ResponseDto<MaterialDetailDto>.SuccessResult(dto);
        }
    }
}
