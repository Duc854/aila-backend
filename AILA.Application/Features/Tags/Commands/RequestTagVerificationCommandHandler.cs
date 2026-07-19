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

            // 2. Chỉ Expert sở hữu mới được gửi yêu cầu
            if (tag.CreatedById != request.ExpertId)
                throw new UnauthorizedAccessException("Bạn không có quyền gửi yêu cầu xét duyệt cho tag này.");

            // 3. Tag đã published rồi thì không cần duyệt nữa
            if (tag.IsPublished)
                throw new InvalidOperationException("Tag này đã được xuất bản.");

            // 4. Kiểm tra không có request Pending đang tồn tại
            if (tag.PublishRequest != null &&
                tag.PublishRequest.Status == Domain.Enums.TagPublishRequestStatus.Pending)
                throw new InvalidOperationException("Đã tồn tại yêu cầu chờ duyệt.");

            // 5. Tạo TagPublishRequest mới và add trực tiếp vào repository.
            // Không dùng domain method Tag.CreatePublishRequest() vì navigation property
            // có UsePropertyAccessMode.Property khiến EF Core không track được thay đổi.
            //var publishRequest = new TagPublishRequest(tag.Id,Guid.NewGuid, request.Note);
            //await _uow.Repository<TagPublishRequest>().AddAsync(publishRequest);

            // 6. Cập nhật UpdatedAt trên Tag
            tag.UpdateTimestamp();

            await _uow.SaveChangesAsync(cancellationToken);

            return new ExpertTagDto();
        }

        //private static ExpertTagDto MapToDto(Domain.Entities.Tag tag) => new()
        //{
        //    Id          = tag.Id,
        //    Name        = tag.Name,
        //    Code        = tag.Code,
        //    IsPublished = tag.IsPublished,
        //    CreatedAt   = tag.CreatedAt,
        //    PublishRequest = tag.PublishRequest is null ? null : new TagPublishRequestDto
        //    {
        //        Id         = tag.PublishRequest.Id,
        //        Status     = tag.PublishRequest.Status.ToString(),
        //        Note       = tag.PublishRequest.Note,
        //        CreatedAt  = tag.PublishRequest.CreatedAt,
        //        ReviewedAt = tag.PublishRequest.ReviewedAt
        //    }
        //};
    }
}
