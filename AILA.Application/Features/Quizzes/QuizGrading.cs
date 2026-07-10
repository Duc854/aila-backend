using AILA.Domain.Entities;

namespace AILA.Application.Features.Quizzes
{
    /// <summary>
    /// Logic chấm điểm dùng chung (server-side). Một câu hỏi có thể chọn nhiều đáp án
    /// (câu hỏi nhiều lựa chọn). Chấm theo kiểu tất-cả-hoặc-không: câu đúng khi
    /// TẬP đáp án được chọn trùng khớp chính xác TẬP đáp án đúng của câu hỏi.
    /// (Áp dụng đồng nhất cho cả SingleChoice và MultipleChoice.)
    /// </summary>
    public static class QuizGrading
    {
        public static bool IsAnswerCorrect(Question question, IEnumerable<Guid> selectedOptionIds)
        {
            var correctIds = question.AnswerOptions
                .Where(o => o.IsCorrect)
                .Select(o => o.Id)
                .ToHashSet();

            if (correctIds.Count == 0)
                return false; // câu hỏi thiếu đáp án đúng → coi như sai (không chấm được)

            var selected = (selectedOptionIds ?? Enumerable.Empty<Guid>()).ToHashSet();
            return selected.Count > 0 && selected.SetEquals(correctIds);
        }

        /// <summary>
        /// Đếm số câu đúng dựa trên map câu hỏi và tập lựa chọn (questionId -> danh sách optionId).
        /// </summary>
        public static int CountCorrect(
            IReadOnlyDictionary<Guid, Question> questionsById,
            IReadOnlyDictionary<Guid, List<Guid>> selectionsByQuestion)
        {
            var correct = 0;
            foreach (var question in questionsById.Values)
            {
                selectionsByQuestion.TryGetValue(question.Id, out var selected);
                if (IsAnswerCorrect(question, selected ?? Enumerable.Empty<Guid>()))
                    correct++;
            }
            return correct;
        }
    }
}
