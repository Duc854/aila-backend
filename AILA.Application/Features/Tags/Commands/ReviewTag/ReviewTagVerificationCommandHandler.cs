using AILA.Domain.Enums;
using MediatR;
using AILA.Application.Common.Interfaces;
using AILA.Application.Common.Notifications;
using AILA.Domain.Entities;
using Shared.Wrappers;

namespace AILA.Application.Features.Tags.Commands.ReviewTagVerifications
{
    public class ReviewTagVerificationCommandHandler
        : IRequestHandler<ReviewTagVerificationCommand, ResponseDto<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public ReviewTagVerificationCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ResponseDto<bool>> Handle(
            ReviewTagVerificationCommand request,
            CancellationToken cancellationToken)
        {
            // Validate
            if (request.TagId == Guid.Empty)
            {
                return ResponseDto<bool>.FailResult(
                    "INVALID_TAG_ID",
                    "Tag ID không hợp lệ.");
            }

            var tag = await _unitOfWork.Tags
       .GetVerificationRequestByIdAsync(request.TagId, cancellationToken);

            if (tag == null)
            {
                return ResponseDto<bool>.FailResult(
                    "TAG_NOT_FOUND",
                    $"Không tìm thấy tag với ID: {request.TagId}");
            }

            // Check if tag has publish request
            if (tag.PublishRequest == null)
            {
                return ResponseDto<bool>.FailResult(
                    "REQUEST_NOT_FOUND",
                    $"Tag '{tag.Name}' không có yêu cầu duyệt.");
            }

            // Check if request is already processed
            if (tag.PublishRequest.Status != TagPublishRequestStatus.Pending)
            {
                return ResponseDto<bool>.FailResult(
                    "INVALID_STATUS",
                    $"Yêu cầu duyệt tag đã ở trạng thái '{tag.PublishRequest.Status}'");
            }

            // Process based on status
            bool isApproved;
            switch (request.Status)
            {
                case TagPublishRequestStatus.Approved:
                    tag.PublishRequest.Approve();
                    tag.Publish();
                    isApproved = true;
                    break;

                case TagPublishRequestStatus.Rejected:
                    if (string.IsNullOrWhiteSpace(request.Note))
                    {
                        return ResponseDto<bool>.FailResult(
                            "MISSING_REJECTION_REASON",
                            "Lý do từ chối là bắt buộc.");
                    }
                    tag.PublishRequest.Reject(request.Note!);
                    isApproved = false;
                    break;

                default:
                    return ResponseDto<bool>.FailResult(
                        "INVALID_STATUS",
                        $"Trạng thái không hợp lệ: {request.Status}");
            }

            // Gửi thông báo kết quả duyệt cho expert đã tạo tag (nếu không phải system tag)
            if (tag.CreatedById.HasValue)
            {
                await _unitOfWork.Notifications.AddAsync(
                    NotificationTemplates.TagVerificationReviewed(
                        tag.CreatedById.Value,
                        tag.Id,
                        tag.Name,
                        isApproved,
                        isApproved ? null : request.Note));
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return ResponseDto<bool>.SuccessResult(true);
        }
    }
}
