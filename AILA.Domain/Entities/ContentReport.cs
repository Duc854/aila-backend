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

        // ✅ Factory Method - Create Course Report
        public static ContentReport CreateCourseReport(
            Guid learnerId,
            Guid courseId,
            ReportType reportType,
            string? description)
        {
            // Validate
            if (learnerId == Guid.Empty)
                throw new ArgumentException("Người báo cáo không hợp lệ.");

            if (courseId == Guid.Empty)
                throw new ArgumentException("Course ID không hợp lệ.");

            if (!Enum.IsDefined(typeof(ReportType), reportType))
                throw new ArgumentException("Loại báo cáo không hợp lệ.");

            if (description?.Length > 1000)
                throw new ArgumentException("Mô tả không được vượt quá 1000 ký tự.");

            return new ContentReport
            {
                Id = Guid.NewGuid(),
                LearnerId = learnerId,
                CourseId = courseId,
                MaterialId = null,
                ReportType = reportType,
                Description = description?.Trim(),
                Status = ReportStatus.Pending,
                ResolvedAt = DateTime.UtcNow
            };
        }

        // ✅ Factory Method - Create Material Report
        public static ContentReport CreateMaterialReport(
            Guid learnerId,
            Guid courseId,
            Guid materialId,
            ReportType reportType,
            string? description)
        {
            // Validate
            if (learnerId == Guid.Empty)
                throw new ArgumentException("Người báo cáo không hợp lệ.");

            if (courseId == Guid.Empty)
                throw new ArgumentException("Course ID không hợp lệ.");

            if (materialId == Guid.Empty)
                throw new ArgumentException("Material ID không hợp lệ.");

            if (!Enum.IsDefined(typeof(ReportType), reportType))
                throw new ArgumentException("Loại báo cáo không hợp lệ.");

            if (description?.Length > 1000)
                throw new ArgumentException("Mô tả không được vượt quá 1000 ký tự.");

            return new ContentReport
            {
                Id = Guid.NewGuid(),
                LearnerId = learnerId,
                CourseId = courseId,
                MaterialId = materialId,
                ReportType = reportType,
                Description = description?.Trim(),
                Status = ReportStatus.Pending,
                ResolvedAt = DateTime.UtcNow
            };
        }

        // ✅ Domain Method - Mark as Resolved (UC-79)
        public void MarkAsResolved()
        {
            // ✅ BR-04: Only Pending can be marked as Resolved
            if (Status != ReportStatus.Pending)
                throw new InvalidOperationException($"Không thể xử lý báo cáo ở trạng thái '{Status}'.");

            Status = ReportStatus.Resolved;
            ResolvedAt = DateTime.UtcNow;
            UpdateTimestamp();
        }
    
        public void UpdateDescription(string? description)
        {
            if (Status == ReportStatus.Resolved)
                throw new InvalidOperationException("Không thể chỉnh sửa báo cáo đã được xử lý.");

            Description = description?.Trim();
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
