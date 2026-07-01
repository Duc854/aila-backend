using AILA.Application.Common.Dtos;
using AILA.Application.Common.Interfaces;
using AILA.Domain.Entities;
using MediatR;
using Shared.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Features.Materials.Queries.GetMaterialDetail
{
    public class GetMaterialDetailQueryHandler : IRequestHandler<GetMaterialDetailQuery, ResponseDto<MaterialDetailDto>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetMaterialDetailQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ResponseDto<MaterialDetailDto>> Handle(GetMaterialDetailQuery request, CancellationToken cancellationToken)
        {
            var material = await _unitOfWork.Materials.GetMaterialDetailAsync(request.CourseId, request.MaterialId);

            if (material == null)
            {
                return ResponseDto<MaterialDetailDto>.FailResult("MATERIAL_NOT_FOUND", "Không tìm thấy học liệu yêu cầu trong khóa học này.");
            }

            // Map Entity sang DTO mẫu (Có thể thay thế bằng AutoMapper nếu dự án của bạn đang dùng)
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
                    ThumbnailUrl = material.VideoDetails.ThumbnailUrl,
                    CaptionsUrl = material.VideoDetails.CaptionsUrl,
                    Content = material.VideoDetails.Content
                } : null,
                DocumentDetails = material.DocumentDetails != null ? new DocumentMaterialDto
                {
                    Content = material.DocumentDetails.Content,
                    DocumentUrl = material.DocumentDetails.DocumentUrl
                } : null
            };

            return ResponseDto<MaterialDetailDto>.SuccessResult(dto);
        }
    }
}
