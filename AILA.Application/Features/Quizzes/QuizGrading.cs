using AILA.Domain.Entities;

namespace AILA.Application.Features.Quizzes
{
    /// <summary>
    /// Logic chấm điểm dùng chung (server-side). Mỗi câu hỏi chỉ chọn một đáp án:
    /// đúng khi đáp án được chọn là đáp án đúng của câu hỏi.
    /// </summary>
    public static class QuizGrading
    {
        public static bool IsSelectionCorrect(Question question, Guid? selectedOptionId)
            => selectedOptionId.HasValue
               && question.AnswerOptions.Any(o => o.Id == selectedOptionId.Value && o.IsCorrect);

        /// <summary>
        /// Đếm số câu đúng dựa trên map câu hỏi và lựa chọn (questionId -> selectedOptionId).
        /// </summary>
        public static int CountCorrect(
            IReadOnlyDictionary<Guid, Question> questionsById,
            IReadOnlyDictionary<Guid, Guid?> selections)
        {
            var correct = 0;
            foreach (var question in questionsById.Values)
            {
                selections.TryGetValue(question.Id, out var selectedOptionId);
                if (IsSelectionCorrect(question, selectedOptionId))
                    correct++;
            }
            return correct;
        }
    }
}
