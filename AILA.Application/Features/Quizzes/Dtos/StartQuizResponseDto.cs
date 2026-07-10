namespace AILA.Application.Features.Quizzes.Dtos
{
    /// <summary>
    /// Dữ liệu trả về khi Learner bắt đầu (hoặc tiếp tục) một lượt làm bài kiểm tra.
    /// KHÔNG chứa thông tin đáp án đúng (IsCorrect) để tránh lộ đáp án ra client trước khi nộp.
    /// </summary>
    public class StartQuizResponseDto
    {
        public Guid AttemptId { get; set; }
        public Guid MaterialId { get; set; }
        public int TimeLimitMinutes { get; set; }
        public decimal PassingScore { get; set; }
        public bool ShowCorrectAnswersAfterSubmission { get; set; }

        /// <summary>Thời điểm bắt đầu lượt làm bài (UTC).</summary>
        public DateTime StartedAt { get; set; }

        /// <summary>Mốc hết giờ do server quyết định (UTC) — dùng cho auto-submit.</summary>
        public DateTime DeadlineAt { get; set; }

        public List<QuizQuestionDto> Questions { get; set; } = new();
    }

    public class QuizQuestionDto
    {
        public Guid QuestionId { get; set; }
        public string Content { get; set; } = string.Empty;
        public string QuestionType { get; set; } = string.Empty;
        public int OrderIndex { get; set; }
        public List<QuizOptionDto> Options { get; set; } = new();
    }

    public class QuizOptionDto
    {
        public Guid OptionId { get; set; }
        public string Content { get; set; } = string.Empty;
        public int OrderIndex { get; set; }
    }
}
