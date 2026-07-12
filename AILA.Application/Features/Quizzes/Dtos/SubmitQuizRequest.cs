namespace AILA.Application.Features.Quizzes.Dtos
{
    /// <summary>
    /// Body request khi Learner nộp bài kiểm tra.
    /// Mỗi câu hỏi có thể chọn một HOẶC nhiều đáp án (câu hỏi nhiều lựa chọn).
    /// </summary>
    public class SubmitQuizRequest
    {
        public List<QuizAnswerSubmissionDto> Answers { get; set; } = new();
    }

    public class QuizAnswerSubmissionDto
    {
        public Guid QuestionId { get; set; }

        /// <summary>Danh sách đáp án được chọn cho câu hỏi. Rỗng nghĩa là bỏ trống (không trả lời).</summary>
        public List<Guid> SelectedOptionIds { get; set; } = new();
    }
}
