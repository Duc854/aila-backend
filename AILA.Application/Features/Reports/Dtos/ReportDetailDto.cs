using System;

namespace AILA.Application.Features.Reports.Dtos
{
    public class ReportDetailDto
    {
        public Guid Id { get; set; }

        // Reported Content (BR-03)
        public Guid CourseId { get; set; }
        public Guid? MaterialId { get; set; }
        public string? CourseName { get; set; }
        public string? MaterialName { get; set; }
        public string? ContentType { get; set; }

        /// True nếu course đang bị lock do report (IsPublicationLocked = true)
        public bool? IsCourseLocked { get; set; }

        // Reporter
        public string? LearnerName { get; set; }
        public string? LearnerEmail { get; set; }

        // Report Details
        public string? Reason { get; set; }
        public string? Description { get; set; }
        public string? Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
    }
}
