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
    /// AverageQuizScore / AverageAiScenarioScore = null khi Learner chưa có lượt nào
    /// (tránh chia cho 0; FE hiển thị "—").
    /// </summary>
    public record LearningSummaryDto(
        int TotalCourses,
        int CoursesInProgress,
        int CoursesCompleted,
        int TotalQuizzesTaken,
        int QuizzesPassed,
        decimal? AverageQuizScore,
        int TotalAiScenariosPracticed,
        decimal? AverageAiScenarioScore
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
    /// Một mục trong lịch sử luyện tập AI scenario (UC-30, AC-5) — mỗi mục là một lượt
    /// thực hành đã HOÀN THÀNH (PracticeAttempt.Completed). Kèm CourseId + MaterialId +
    /// AttemptId để FE liên kết sang màn xem kết quả chi tiết lượt thực hành.
    /// Score = null nếu lượt đó được lưu mà chưa có điểm tổng (dữ liệu cũ).
    /// </summary>
    public record AiScenarioHistoryItemDto(
        Guid AttemptId,
        Guid CourseId,
        Guid MaterialId,
        string ScenarioName,
        string CourseName,
        string? Difficulty,
        decimal? Score,
        DateTime StartedAt,
        DateTime? CompletedAt
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
