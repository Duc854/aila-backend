using AILA.Domain.Common;
using AILA.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Domain.Entities
{
    public class CourseReviewRequest : BaseEntity
    {
        public Guid CourseId { get; private set; }

        public string Reason { get; private set; }

        public CourseReviewRequestStatus Status { get; private set; }

        public string? ReviewComment { get; private set; }

        public DateTime? ReviewedAt { get; private set; }

        // Navigation
        public virtual Course Course { get; private set; }

        private CourseReviewRequest()
        {
        }

        public CourseReviewRequest(
            Guid courseId,
            string reason)
        {
            if (courseId == Guid.Empty)
                throw new ArgumentException("Mã khóa học không hợp lệ.");

            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("Lý do yêu cầu không được để trống.");

            Id = Guid.NewGuid();

            CourseId = courseId;
            Reason = reason.Trim();
            Status = CourseReviewRequestStatus.Pending;
        }

        /// <summary>
        /// Admin phê duyệt yêu cầu review lại khóa học
        /// </summary>
        public void Approve(string? reviewComment)
        {
            if (Status != CourseReviewRequestStatus.Pending)
                throw new InvalidOperationException("Yêu cầu đã được xử lý.");

            Status = CourseReviewRequestStatus.Approved;
            ReviewComment = reviewComment?.Trim();
            ReviewedAt = DateTime.UtcNow;
            UpdateTimestamp();
        }

        /// <summary>
        /// Admin từ chối yêu cầu mở khóa khóa học
        /// </summary>
        public void Reject( string reviewComment)
        {
            if (Status != CourseReviewRequestStatus.Pending)
                throw new InvalidOperationException("Yêu cầu đã được xử lý.");

            if (string.IsNullOrWhiteSpace(reviewComment))
                throw new ArgumentException("Lý do từ chối không được để trống.");

            Status = CourseReviewRequestStatus.Rejected;

            ReviewComment = reviewComment.Trim();
            ReviewedAt = DateTime.UtcNow;
            UpdateTimestamp();
        }
    }
}
