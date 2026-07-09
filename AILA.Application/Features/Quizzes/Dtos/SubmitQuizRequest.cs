namespace AILA.Application.Features.Quizzes.Dtos
{
    /// <summary>
    /// Body request khi Learner nộp bài kiểm tra.
    /// Mỗi câu hỏi chỉ chọn duy nhất một đáp án.
    /// </summary>
    public class SubmitQuizRequest
    {
        public List<QuizAnswerSubmissionDto> Answers { get; set; } = new();
    }

    public class QuizAnswerSubmissionDto
    {
        public Guid QuestionId { get; set; }

        /// <summary>Đáp án được chọn cho câu hỏi. Null nghĩa là bỏ trống (không trả lời).</summary>
        public Guid? SelectedOptionId { get; set; }
    }
}
