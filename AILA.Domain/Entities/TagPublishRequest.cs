using AILA.Domain.Common;
using AILA.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Domain.Entities
{
    public class TagPublishRequest : BaseEntity
    {
        public Guid TagId { get; private set; }

        public string? Note { get; private set; }

        public TagPublishRequestStatus Status { get; private set; }

        public DateTime? ReviewedAt { get; private set; }

        // Navigation
        public virtual Tag Tag { get; private set; } = null!;

        private TagPublishRequest() { }

        public TagPublishRequest(Guid tagId, string? note)
        {
            if (tagId == Guid.Empty)
                throw new ArgumentException("Tag không hợp lệ.");

            Id = Guid.NewGuid();
            TagId = tagId;
            Note = string.IsNullOrWhiteSpace(note)
                ? null
                : note.Trim();

            Status = TagPublishRequestStatus.Pending;
        }

        /// <summary>
        /// Admin phê duyệt yêu cầu.
        /// </summary>
        public void Approve()
        {
            if (Status != TagPublishRequestStatus.Pending)
                throw new InvalidOperationException("Yêu cầu này đã được xử lý.");

            Status = TagPublishRequestStatus.Approved;
            ReviewedAt = DateTime.UtcNow;

            UpdateTimestamp();
        }

        /// <summary>
        /// Admin từ chối yêu cầu.
        /// </summary>
        public void Reject(string reason)
        {
            if (Status != TagPublishRequestStatus.Pending)
                throw new InvalidOperationException("Yêu cầu này đã được xử lý.");

            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("Lý do từ chối không được để trống.");

            Status = TagPublishRequestStatus.Rejected;
            Note = reason.Trim();
            ReviewedAt = DateTime.UtcNow;

            UpdateTimestamp();
        }
    }
}
