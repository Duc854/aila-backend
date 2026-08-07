using System;
using System.Collections.Generic;

namespace AILA.Application.Features.Experts.Dtos
{
    public class ExpertDashboardDto
    {
        public bool HasPublishedCourses { get; set; }
        public bool HasData { get; set; }
        public string? Message { get; set; }

        public DashboardOverviewStatsDto Overview { get; set; } = new();
        public List<TrendPointDto> Trends { get; set; } = new();
        public List<CoursePerformanceSummaryDto> CoursePerformances { get; set; } = new();
        public List<CourseOptionDto> AvailableCourses { get; set; } = new();
    }

    public class DashboardOverviewStatsDto
    {
        public int TotalPublishedCourses { get; set; }
        public int TotalEnrollments { get; set; }
        public int TotalActiveLearners { get; set; }
        public decimal AverageCompletionRate { get; set; }
        public int CompletedEnrollmentsCount { get; set; }
        public int TotalQuizAttempts { get; set; }
        public int TotalPracticeAttempts { get; set; }
    }

    public class TrendPointDto
    {
        public string DateLabel { get; set; } = string.Empty;
        public int Enrollments { get; set; }
        public int ActiveLearners { get; set; }
    }

    public class CoursePerformanceSummaryDto
    {
        public Guid CourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public int TotalEnrollments { get; set; }
        public decimal CompletionRate { get; set; }
    }

    public class CourseOptionDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
