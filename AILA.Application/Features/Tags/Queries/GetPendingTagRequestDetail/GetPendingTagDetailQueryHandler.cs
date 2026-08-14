using AILA.Application.Common.Interfaces;
using AILA.Application.Features.Tags.Dtos;
using AILA.Domain.Constants;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using MediatR;
using Shared.Wrappers;
using System;
using System.Threading;
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

            if (tag == null || tag.PublishRequest == null)
            {
                return ResponseDto<TagDto>.FailResult(
                    "NOT_FOUND",
                    "Không tìm thấy yêu cầu xác minh tag.");
            }

            if (tag.PublishRequest.Status != TagPublishRequestStatus.Pending)
            {
                return ResponseDto<TagDto>.FailResult(
                    "INVALID_STATUS",
                    $"Yêu cầu xác minh tag không ở trạng thái chờ duyệt (Hiện tại: {tag.PublishRequest.Status}).");
            }

            // Ưu tiên lấy RequestedById từ PublishRequest, nếu không có mới dùng CreatedById của Tag
            var submitterId = tag.PublishRequest.RequestedById != Guid.Empty
                ? tag.PublishRequest.RequestedById
                : tag.CreatedById;

            User? user = null;
            if (submitterId.HasValue && submitterId.Value != Guid.Empty)
            {
                user = await uow.Users.GetByIdAsync(submitterId.Value);
            }

            var usageCount = await uow.Tags.GetUsageCountAsync(tag.Id, ct);
            bool isReserved = ReservedTagCodes.All.Contains(tag.Code);

            var result = new TagDto
            {
                Id = tag.Id,
                Name = tag.Name,
                Code = tag.Code,
                IsPublished = tag.IsPublished,
                CreatedById = tag.CreatedById,

                SubmittedBy = user?.FullName ?? "Hệ thống",
                RequestStatus = tag.PublishRequest.Status,
                SubmittedAt = tag.PublishRequest.CreatedAt,
                Note = tag.PublishRequest.RequestNote,

                Source = tag.CreatedById.HasValue ? "Expert" : "System",
                UsageCount = usageCount,
                IsReserved = isReserved
            };

            return ResponseDto<TagDto>.SuccessResult(result);
        }
    }
}
