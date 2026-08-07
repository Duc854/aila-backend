using AILA.Domain.Common;
using AILA.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Domain.Entities
{
    public class ContentReport : BaseEntity
    {
        public Guid LearnerId { get; private set; }

        // Every report belongs to a course
        public Guid CourseId { get; private set; }

        // Optional: report a specific learning material
        public Guid? MaterialId { get; private set; }

        public ReportType ReportType { get; private set; }

        public string? Description { get; private set; }

        public ReportStatus Status { get; private set; }

        public DateTime? ResolvedAt { get; private set; }


        // Navigation
        public virtual Learner Learner { get; private set; } = null!;

        public virtual Course Course { get; private set; } = null!;

        public virtual Material? Material { get; private set; }


        private ContentReport() { }


        public ContentReport(
            Guid learnerId,
            Guid courseId,
            Guid? materialId,
            ReportType reportType,
            string? description)
        {
            if (learnerId == Guid.Empty)
                throw new ArgumentException(
                    "Người báo cáo không hợp lệ.");

            if (courseId == Guid.Empty)
                throw new ArgumentException(
                    "Khóa học không hợp lệ.");

            Id = Guid.NewGuid();

            LearnerId = learnerId;
            CourseId = courseId;
            MaterialId = materialId;

            ReportType = reportType;
            Description = description?.Trim();

            Status = ReportStatus.Pending;
        }


        public void UpdateDescription(string? description)
        {
            if (Status != ReportStatus.Pending)
                throw new InvalidOperationException(
                    "Chỉ có thể chỉnh sửa báo cáo đang chờ xử lý.");

            Description = description?.Trim();

            UpdateTimestamp();
        }


        /// <summary>
        /// Admin xử lý báo cáo.
        /// </summary>
        public void Resolve()
        {
            if (Status == ReportStatus.Resolved)
                throw new InvalidOperationException(
                    "Báo cáo đã được xử lý.");

            Status = ReportStatus.Resolved;
            ResolvedAt = DateTime.UtcNow;

            UpdateTimestamp();
        }


        /// <summary>
        /// Mở lại báo cáo đã xử lý.
        /// </summary>
        public void Reopen()
        {
            if (Status != ReportStatus.Resolved)
                throw new InvalidOperationException(
                    "Chỉ có thể mở lại báo cáo đã xử lý.");

            Status = ReportStatus.Pending;
            ResolvedAt = null;

            UpdateTimestamp();
        }
    }
}
