using AILA.Application.Common.Dtos;
using AILA.Application.Common.Interfaces;
using AILA.Domain.Entities;
using MediatR;

namespace AILA.Application.Features.Tags.Commands
{
    public class RequestTagVerificationCommandHandler
        : IRequestHandler<RequestTagVerificationCommand, ExpertTagDto>
    {
        private readonly IUnitOfWork _uow;

        public RequestTagVerificationCommandHandler(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<ExpertTagDto> Handle(
            RequestTagVerificationCommand request,
            CancellationToken cancellationToken)
        {
            // 1. Lấy tag kèm PublishRequest để kiểm tra trạng thái
            var tag = await _uow.Tags.GetWithPublishRequestAsync(request.TagId, cancellationToken);
            if (tag == null)
                throw new InvalidOperationException("Tag không tồn tại.");

            // 2. Tag đã published rồi thì không cần duyệt nữa
            if (tag.IsPublished)
                throw new InvalidOperationException("Tag này đã được xuất bản.");

            if (tag.PublishRequest is null)
            {
                // Chưa có request nào → tạo mới
                var publishRequest = TagPublishRequest.Create(tag.Id, request.ExpertId, request.Note);
                await _uow.Repository<TagPublishRequest>().AddAsync(publishRequest);
            }
            else if (tag.PublishRequest.Status == Domain.Enums.TagPublishRequestStatus.Pending)
            {
                throw new InvalidOperationException("Đã tồn tại yêu cầu chờ duyệt.");
            }
            else if (tag.PublishRequest.Status == Domain.Enums.TagPublishRequestStatus.Rejected)
            {
                // Gửi lại sau khi bị từ chối
                tag.PublishRequest.Resubmit(request.ExpertId, request.Note);
            }

            tag.UpdateTimestamp();

            await _uow.SaveChangesAsync(cancellationToken);

            // Reload để lấy PublishRequest mới nhất
            var updated = await _uow.Tags.GetWithPublishRequestAsync(request.TagId, cancellationToken);

            return MapToDto(updated!);
        }

        private static ExpertTagDto MapToDto(Tag tag) => new()
        {
            Id          = tag.Id,
            Name        = tag.Name,
            Code        = tag.Code,
            IsPublished = tag.IsPublished,
            CreatedAt   = tag.CreatedAt,
            PublishRequest = tag.PublishRequest is null ? null : new TagPublishRequestDto
            {
                Id            = tag.PublishRequest.Id,
                Status        = tag.PublishRequest.Status.ToString(),
                RequestNote   = tag.PublishRequest.RequestNote,
                ReviewComment = tag.PublishRequest.ReviewComment,
                CreatedAt     = tag.PublishRequest.CreatedAt,
                ReviewedAt    = tag.PublishRequest.ReviewedAt
            }
        };
    }
}
