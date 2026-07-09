using AILA.Application.Features.Profile.Dtos;
using AILA.Domain.Entities;

namespace AILA.Application.Features.Profile.Mappers
{
    /// <summary>
    /// Ánh xạ dữ liệu học tập của Learner sang DTO (UC-30). Dùng chung cho màn Profile
    /// (chỉ 5 mục mới nhất mỗi khối) và các endpoint "xem tất cả" (có phân trang).
    /// </summary>
    public static class LearnerLearningMapper
    {
        public static EnrollmentSummaryDto ToSummaryDto(this Enrollment e) => new(
            e.CourseId,
            e.Course.Name,
            e.Course.ThumbnailUrl,
            e.Course.Category?.Name ?? "",
            e.Course.Description,
            (double)e.Course.DurationHours,
            e.Status.ToString(),
            (int)e.ProgressPct,
            e.TotalMaterials,
            e.CompletedMaterials,
            e.EnrolledAt,
            e.CompletedAt,
            e.LastAccessedAt
        );

        public static QuizHistoryItemDto ToHistoryDto(this QuizAttempt a) => new(
            a.Id,
            a.Enrollment.CourseId,
            a.QuizMaterialId,
            a.QuizMaterial.Material.Title,
            a.Enrollment.Course.Name,
            a.Score,
            a.IsPassed,
            a.StartedAt,
            a.SubmittedAt
        );
    }
}
