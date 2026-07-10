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

        public Guid? CourseId { get; private set; }

        public Guid? MaterialId { get; private set; }

        public ReportType ReportType { get; private set; }

        public string? Description { get; private set; }

        public ReportStatus Status { get; private set; }

        public DateTime? ResolvedAt { get; private set; }

        // Navigation
        public virtual Learner Learner { get; private set; } = null!;

        public virtual Course? Course { get; private set; }

        public virtual Material? Material { get; private set; }

        private ContentReport() { }

        public ContentReport(
            Guid learnerId,
            Guid? courseId,
            Guid? materialId,
            ReportType reportType,
            string? description)
        {
            if (learnerId == Guid.Empty)
                throw new ArgumentException("Người báo cáo không hợp lệ.");

            if (courseId == null && materialId == null)
                throw new ArgumentException("Báo cáo phải thuộc một khóa học hoặc một học liệu.");

            if (courseId != null && materialId != null)
                throw new ArgumentException("Chỉ được báo cáo một đối tượng mỗi lần.");

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
            if (Status == ReportStatus.Resolved)
                throw new InvalidOperationException("Không thể chỉnh sửa báo cáo đã được xử lý.");

            Description = description?.Trim();
            UpdateTimestamp();
        }

        /// <summary>
        /// Admin đánh dấu báo cáo đã xử lý.
        /// </summary>
        public void Resolve()
        {
            if (Status == ReportStatus.Resolved)
                throw new InvalidOperationException("Báo cáo đã được xử lý.");

            Status = ReportStatus.Resolved;
            ResolvedAt = DateTime.UtcNow;

            UpdateTimestamp();
        }


        /// <summary>
        /// Admin mở lại báo cáo đã xử lý trong trường hợp ấn nhầm.
        /// </summary>
        public void Reopen()
        {
            if (Status == ReportStatus.Pending)
                throw new InvalidOperationException("Báo cáo chưa được xử lý, không thể mở lại.");

            Status = ReportStatus.Pending;
            ResolvedAt = DateTime.UtcNow;

            UpdateTimestamp();
        }
    }
}
