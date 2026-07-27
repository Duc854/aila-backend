using AILA.Application.Common.Interfaces;
using AILA.Application.Features.Tags.Dtos;
using AILA.Domain.Entities;
using MediatR;
using Shared.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Features.Tags.Queries.GetPendingTagDetail
{
    public class GetPendingTagDetailQueryHandler(IUnitOfWork uow)
        : IRequestHandler<GetPendingTagDetailQuery, ResponseDto<TagDto>>
    {
        public async Task<ResponseDto<TagDto>> Handle(
      GetPendingTagDetailQuery request,
      CancellationToken ct)
        {
            var tag = await uow.Tags.GetVerificationRequestByIdAsync(request.TagId, ct);

            if (tag == null)
            {
                return ResponseDto<TagDto>.FailResult(
                    "NOT_FOUND",
                    "Không tìm thấy tag.");
            }

            User? user = null;

            if (tag.CreatedById.HasValue)
            {
                user = await uow.Users.GetByIdAsync(tag.CreatedById.Value);
            }

            var result = new TagDto
            {
                Id = tag.Id,
                Name = tag.Name,
                Code = tag.Code,
                IsPublished = tag.IsPublished,
                CreatedById = tag.CreatedById,

                SubmittedBy = user?.FullName,
                RequestStatus = tag.PublishRequest?.Status,
                SubmittedAt = tag.PublishRequest?.CreatedAt,
                Note = tag.PublishRequest?.RequestNote,

                Source = "Expert",
                UsageCount = 0
            };

            return ResponseDto<TagDto>.SuccessResult(result);
        }
    }
}