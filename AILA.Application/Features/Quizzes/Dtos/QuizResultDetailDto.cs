namespace AILA.Application.Features.Quizzes.Dtos
{
    /// <summary>
    /// Chi tiết kết quả bài kiểm tra (UC-27, AC-3): review từng câu + đáp án đúng.
    /// Chỉ trả về khi quiz cấu hình cho phép hiển thị đáp án (BR-01).
    /// </summary>
    public class QuizResultDetailDto
    {
        public Guid AttemptId { get; set; }
        public decimal Score { get; set; }
        public bool IsPassed { get; set; }
        public decimal PassingScore { get; set; }
        public int TotalQuestions { get; set; }
        public int CorrectAnswers { get; set; }
        public DateTime? SubmittedAt { get; set; }

        public List<QuizResultQuestionDto> Questions { get; set; } = new();
    }

    public class QuizResultQuestionDto
    {
        public Guid QuestionId { get; set; }
        public string Content { get; set; } = string.Empty;
        public string QuestionType { get; set; } = string.Empty;
        public int OrderIndex { get; set; }

        /// <summary>Đáp án Learner đã chọn (null nếu bỏ trống).</summary>
        public Guid? SelectedOptionId { get; set; }

        /// <summary>Lựa chọn của Learner có đúng không.</summary>
        public bool IsCorrect { get; set; }

        public List<QuizResultOptionDto> Options { get; set; } = new();
    }

    public class QuizResultOptionDto
    {
        public Guid OptionId { get; set; }
        public string Content { get; set; } = string.Empty;
        public int OrderIndex { get; set; }

        /// <summary>Đáp án đúng — chỉ lộ ra ở màn review khi quiz cho phép.</summary>
        public bool IsCorrect { get; set; }

        /// <summary>Đây có phải đáp án Learner đã chọn không.</summary>
        public bool IsSelected { get; set; }
    }
}
