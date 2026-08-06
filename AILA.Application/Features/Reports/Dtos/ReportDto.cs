using System;

namespace AILA.Application.Features.Reports.Dtos
{
    public class ReportDto
    {
        public Guid Id { get; set; }
        public Guid? CourseId { get; set; }
        public Guid? MaterialId { get; set; }
        public string? CourseName { get; set; }
        public string? MaterialName { get; set; }
        public string? ContentType { get; set; } // "Course" hoặc "Learning Material" (BR-03)
        public bool? IsCourseLocked { get; set; }
        public string? LearnerName { get; set; }
        public string? Reason { get; set; }
        public string? Description { get; set; }
        public string? Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
    }
}
