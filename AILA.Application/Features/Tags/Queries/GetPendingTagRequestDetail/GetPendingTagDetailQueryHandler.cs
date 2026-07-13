using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AILA.Application.Common.Interfaces;
using AILA.Application.Features.Tags.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Tags.Queries.GetPendingTagDetail
{
    public class GetPendingTagDetailQueryHandler(IUnitOfWork uow)
        : IRequestHandler<GetPendingTagDetailQuery, ResponseDto<TagDto>>
    {
        public async Task<ResponseDto<TagDto>> Handle(
            GetPendingTagDetailQuery request,
            CancellationToken ct)
        {
            var tag = await uow.Tags.GetByIdAsync(request.TagId);


            if (tag == null)
            {
                return ResponseDto<TagDto>.FailResult(
                    "NOT_FOUND",
                    "Không tìm thấy tag.");
            }


            if (tag.IsPublished)
            {
                return ResponseDto<TagDto>.FailResult(
                    "NOT_PENDING",
                    "Tag không ở trạng thái chờ duyệt.");
            }


            if (tag.PublishRequest == null)
            {
                return ResponseDto<TagDto>.FailResult(
                    "NO_REQUEST",
                    "Tag chưa có yêu cầu xuất bản.");
            }


            var result = new TagDto
            {
                Id = tag.Id,
                Name = tag.Name,
                Code = tag.Code,
                IsPublished = tag.IsPublished,
                CreatedById = tag.CreatedById,
                Source = "Expert",
                UsageCount = 0
            };


            return ResponseDto<TagDto>
                .SuccessResult(result);
        }
    }
}