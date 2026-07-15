using AILA.Application.Common.Dtos;
using AILA.Application.Common.Interfaces;
using AILA.Domain.Entities;
using MediatR;

namespace AILA.Application.Features.Tags.Commands
{
    public class DeleteTagPublishRequestCommandHandler
        : IRequestHandler<DeleteTagPublishRequestCommand, ExpertTagDto>
    {
        private readonly IUnitOfWork _uow;

        public DeleteTagPublishRequestCommandHandler(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<ExpertTagDto> Handle(
            DeleteTagPublishRequestCommand request,
            CancellationToken cancellationToken)
        {
            // 1. Lấy tag kèm PublishRequest
            var tag = await _uow.Tags.GetWithPublishRequestAsync(request.TagId, cancellationToken);
            if (tag is null)
                throw new InvalidOperationException("Tag không tồn tại.");

            // 2. Chỉ Expert sở hữu tag mới được hủy yêu cầu
            if (tag.CreatedById != request.ExpertId)
                throw new UnauthorizedAccessException("Bạn không có quyền thao tác với tag này.");

            // 3. Phải có yêu cầu đang Pending mới được xóa
            if (tag.PublishRequest is null)
                throw new InvalidOperationException("Tag này chưa có yêu cầu xét duyệt nào.");

            if (tag.PublishRequest.Status != Domain.Enums.TagPublishRequestStatus.Pending)
                throw new InvalidOperationException("Chỉ có thể hủy yêu cầu đang ở trạng thái chờ duyệt.");

            // 4. Xóa TagPublishRequest
            _uow.Repository<TagPublishRequest>().Delete(tag.PublishRequest);

            // 5. Cập nhật timestamp trên Tag
            tag.UpdateTimestamp();

            await _uow.SaveChangesAsync(cancellationToken);

            return new ExpertTagDto
            {
                Id             = tag.Id,
                Name           = tag.Name,
                Code           = tag.Code,
                IsPublished    = tag.IsPublished,
                CreatedAt      = tag.CreatedAt,
                PublishRequest = null
            };
        }
    }
}
