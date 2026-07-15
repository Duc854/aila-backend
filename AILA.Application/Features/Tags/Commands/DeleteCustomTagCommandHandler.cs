using AILA.Application.Common.Interfaces;
using AILA.Domain.Entities;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Tags.Commands
{
    public class DeleteCustomTagCommandHandler
        : IRequestHandler<DeleteCustomTagCommand, ResponseDto<object>>
    {
        private readonly IUnitOfWork _uow;

        public DeleteCustomTagCommandHandler(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<ResponseDto<object>> Handle(
            DeleteCustomTagCommand request,
            CancellationToken cancellationToken)
        {
            // 1. Lấy tag kèm PublishRequest
            var tag = await _uow.Tags.GetWithPublishRequestAsync(request.TagId, cancellationToken);
            if (tag is null)
                throw new InvalidOperationException("Tag không tồn tại.");

            // 2. Chỉ Expert sở hữu tag mới được xóa
            if (tag.CreatedById != request.ExpertId)
                throw new UnauthorizedAccessException("Bạn không có quyền xóa tag này.");

            // 3. Chỉ được xóa tag chưa publish
            if (tag.IsPublished)
                throw new InvalidOperationException("Không thể xóa tag đã được duyệt và xuất bản.");

            // 4. Không được xóa nếu tag đang được gán vào khóa học
            var isAssigned = await _uow.Tags.IsAssignedToCourseAsync(request.TagId, cancellationToken);
            if (isAssigned)
                throw new InvalidOperationException("Không thể xóa tag đang được sử dụng trong khóa học.");

            // 5. Nếu còn PublishRequest thì xóa trước (tránh lỗi FK)
            if (tag.PublishRequest is not null)
                _uow.Repository<TagPublishRequest>().Delete(tag.PublishRequest);

            // 6. Xóa tag
            _uow.Tags.Delete(tag);

            await _uow.SaveChangesAsync(cancellationToken);

            return ResponseDto<object>.SuccessResult(null!);
        }
    }
}
