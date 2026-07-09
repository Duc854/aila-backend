using AILA.Application.Common.Dtos;
using AILA.Application.Common.Interfaces;
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
            // 1. Lấy tag kèm PublishRequest (có tracking để lưu thay đổi)
            var tag = await _uow.Tags.GetWithPublishRequestAsync(request.TagId, cancellationToken);
            if (tag == null)
                throw new InvalidOperationException("Tag không tồn tại.");

            // 2. Chỉ Expert sở hữu mới được gửi yêu cầu
            if (tag.CreatedById != request.ExpertId)
                throw new UnauthorizedAccessException("Bạn không có quyền gửi yêu cầu xét duyệt cho tag này.");

            // 3. Tag đã published rồi thì không cần duyệt nữa
            if (tag.IsPublished)
                throw new InvalidOperationException("Tag này đã được xuất bản.");

            // 4. Gọi domain method — tự kiểm tra Pending trùng bên trong
            tag.CreatePublishRequest(request.Note);

            _uow.Tags.Update(tag);
            await _uow.SaveChangesAsync(cancellationToken);

            return MapToDto(tag);
        }

        private static ExpertTagDto MapToDto(Domain.Entities.Tag tag) => new()
        {
            Id          = tag.Id,
            Name        = tag.Name,
            Code        = tag.Code,
            IsPublished = tag.IsPublished,
            CreatedAt   = tag.CreatedAt,
            PublishRequest = tag.PublishRequest is null ? null : new TagPublishRequestDto
            {
                Id         = tag.PublishRequest.Id,
                Status     = tag.PublishRequest.Status.ToString(),
                Note       = tag.PublishRequest.Note,
                CreatedAt  = tag.PublishRequest.CreatedAt,
                ReviewedAt = tag.PublishRequest.ReviewedAt
            }
        };
    }
}
