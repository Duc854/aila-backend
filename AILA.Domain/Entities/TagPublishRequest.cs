using AILA.Domain.Common;
using AILA.Domain.Enums;

namespace AILA.Domain.Entities
{
    public class TagPublishRequest : BaseEntity
    {
        /// <summary>
        /// Tag cần được xuất bản.
        /// </summary>
        public Guid TagId { get; private set; }

        /// <summary>
        /// Expert gửi yêu cầu hiện tại.
        /// Có thể khác với người tạo Tag ban đầu.
        /// </summary>
        public Guid RequestedById { get; private set; }

        /// <summary>
        /// Ghi chú của Expert khi gửi yêu cầu.
        /// </summary>
        public string? RequestNote { get; private set; }

        /// <summary>
        /// Trạng thái yêu cầu.
        /// </summary>
        public TagPublishRequestStatus Status { get; private set; }

        /// <summary>
        /// Ghi chú phản hồi của Admin khi từ chối.
        /// </summary>
        public string? ReviewComment { get; private set; }

        /// <summary>
        /// Thời điểm Admin xử lý yêu cầu.
        /// </summary>
        public DateTime? ReviewedAt { get; private set; }

        // Navigation
        public virtual Tag Tag { get; private set; } = null!;

        public virtual User RequestedBy { get; private set; } = null!;

        private TagPublishRequest() { }

        private TagPublishRequest(
            Guid tagId,
            Guid requestedById,
            string? requestNote)
        {
            if (tagId == Guid.Empty)
                throw new ArgumentException("Tag không hợp lệ.", nameof(tagId));

            if (requestedById == Guid.Empty)
                throw new ArgumentException("Người gửi yêu cầu không hợp lệ.", nameof(requestedById));

            Id = Guid.NewGuid();

            TagId = tagId;
            RequestedById = requestedById;

            RequestNote = string.IsNullOrWhiteSpace(requestNote)
                ? null
                : requestNote.Trim();

            Status = TagPublishRequestStatus.Pending;
        }

        /// <summary>
        /// Tạo yêu cầu xuất bản đầu tiên.
        /// </summary>
        public static TagPublishRequest Create(
            Guid tagId,
            Guid requestedById,
            string? requestNote)
        {
            return new TagPublishRequest(
                tagId,
                requestedById,
                requestNote);
        }

        /// <summary>
        /// Gửi lại yêu cầu sau khi bị từ chối.
        /// </summary>
        public void Resubmit(
            Guid requestedById,
            string? requestNote)
        {
            if (Status != TagPublishRequestStatus.Rejected)
                throw new InvalidOperationException(
                    "Chỉ yêu cầu đã bị từ chối mới được gửi lại.");

            if (requestedById == Guid.Empty)
                throw new ArgumentException(
                    "Người gửi yêu cầu không hợp lệ.",
                    nameof(requestedById));

            RequestedById = requestedById;

            RequestNote = string.IsNullOrWhiteSpace(requestNote)
                ? null
                : requestNote.Trim();

            ReviewComment = null;
            ReviewedAt = null;
            Status = TagPublishRequestStatus.Pending;

            UpdateTimestamp();
        }

        /// <summary>
        /// Admin phê duyệt yêu cầu.
        /// </summary>
        public void Approve()
        {
            EnsurePending();

            Status = TagPublishRequestStatus.Approved;
            ReviewedAt = DateTime.UtcNow;

            UpdateTimestamp();
        }

        /// <summary>
        /// Admin từ chối yêu cầu.
        /// </summary>
        public void Reject(string reviewComment)
        {
            EnsurePending();

            if (string.IsNullOrWhiteSpace(reviewComment))
                throw new ArgumentException(
                    "Lý do từ chối không được để trống.",
                    nameof(reviewComment));

            Status = TagPublishRequestStatus.Rejected;
            ReviewComment = reviewComment.Trim();
            ReviewedAt = DateTime.UtcNow;

            UpdateTimestamp();
        }

        private void EnsurePending()
        {
            if (Status != TagPublishRequestStatus.Pending)
                throw new InvalidOperationException(
                    "Yêu cầu này đã được xử lý.");
        }
    }
}