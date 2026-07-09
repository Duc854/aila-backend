namespace AILA.Application.Features.Profile.Dtos
{
    public record TagDto(Guid Id, string Name);

    public record LearnerInfoDto(
        string? LearnerType,
        string? KnowledgeLevel,
        bool HasCompletedOnboarding,
        IEnumerable<TagDto> LearningGoals
    );

    public record EnrollmentSummaryDto(
        Guid CourseId,
        string CourseName,
        string? ThumbnailUrl,
        string CategoryName,
        string? Description,
        double DurationHours,
        string Status,
        int ProgressPct,
        int TotalMaterials,
        int CompletedMaterials,
        DateTime EnrolledAt,
        DateTime? CompletedAt,
        DateTime? LastAccessedAt
    );

    /// <summary>
    /// Thống kê tóm tắt (UC-30, AC-2). Chỉ đọc, tính từ dữ liệu đã lưu.
    /// AverageQuizScore = null khi Learner chưa làm quiz nào (tránh chia cho 0; FE hiển thị "—").
    /// </summary>
    public record LearningSummaryDto(
        int TotalCourses,
        int CoursesInProgress,
        int CoursesCompleted,
        int TotalQuizzesTaken,
        int QuizzesPassed,
        decimal? AverageQuizScore
    );

    /// <summary>
    /// Một mục trong lịch sử làm quiz (UC-30, AC-4). Kèm CourseId + MaterialId để FE
    /// liên kết sang màn xem kết quả chi tiết (UC-27).
    /// </summary>
    public record QuizHistoryItemDto(
        Guid AttemptId,
        Guid CourseId,
        Guid MaterialId,
        string QuizTitle,
        string CourseName,
        decimal Score,
        bool IsPassed,
        DateTime StartedAt,
        DateTime? SubmittedAt
    );

    /// <summary>
    /// Một mục trong lịch sử luyện tập AI scenario (UC-30, AC-5).
    /// Hiện chưa có nguồn dữ liệu upstream (luồng AI practice chưa lưu bản ghi),
    /// nên danh sách này tạm rỗng — khối hiển thị empty chứ không lỗi.
    /// </summary>
    public record AiScenarioHistoryItemDto(
        Guid MaterialId,
        string ScenarioName,
        DateTime PerformedAt,
        decimal? Score
    );

    public record LearnerProfileDto(
        Guid Id,
        string FullName,
        string Email,
        string? AvatarUrl,
        string Role,
        LearnerInfoDto Learner,
        IEnumerable<EnrollmentSummaryDto> Enrollments,
        LearningSummaryDto Summary,
        IEnumerable<QuizHistoryItemDto> QuizHistory,
        IEnumerable<AiScenarioHistoryItemDto> AiScenarioHistory
    );
}
