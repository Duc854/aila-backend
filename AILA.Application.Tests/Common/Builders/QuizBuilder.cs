using AILA.Domain.Entities;
using AILA.Domain.Enums;

namespace AILA.Application.Tests.Common.Builders
{
    /// <summary>
    /// Dựng <see cref="QuizMaterial"/> kèm Question → AnswerOption qua đúng API domain.
    /// Mỗi câu hỏi được khai báo bằng số đáp án và (các) vị trí đáp án đúng, để test chấm
    /// điểm biết trước đáp án nào là đúng mà không phải dò.
    /// </summary>
    public class QuizBuilder
    {
        private int _timeLimitMinutes = 30;
        private decimal _passingScore = 70;
        private bool _showAnswers = true;
        private Guid _materialId = Guid.NewGuid();

        private readonly List<(int OptionCount, int[] CorrectIndexes)> _questions = new();

        public QuizBuilder ForMaterial(Guid materialId) { _materialId = materialId; return this; }
        public QuizBuilder WithTimeLimit(int minutes) { _timeLimitMinutes = minutes; return this; }
        public QuizBuilder WithPassingScore(decimal score) { _passingScore = score; return this; }
        public QuizBuilder HideAnswers() { _showAnswers = false; return this; }

        /// <summary>Thêm một câu hỏi; <paramref name="correctIndexes"/> tính từ 0.</summary>
        public QuizBuilder WithQuestion(int optionCount = 4, params int[] correctIndexes)
        {
            _questions.Add((optionCount, correctIndexes.Length == 0 ? new[] { 0 } : correctIndexes));
            return this;
        }

        /// <summary>Câu hỏi KHÔNG có đáp án đúng nào — dùng cho nhánh QUIZ_NOT_CONFIGURED.</summary>
        public QuizBuilder WithQuestionWithoutCorrectAnswer(int optionCount = 4)
        {
            _questions.Add((optionCount, Array.Empty<int>()));
            return this;
        }

        /// <summary>Câu hỏi KHÔNG có đáp án nào — dùng cho nhánh quiz chưa cấu hình.</summary>
        public QuizBuilder WithQuestionWithoutOptions()
        {
            _questions.Add((0, Array.Empty<int>()));
            return this;
        }

        public QuizMaterial Build()
        {
            var quiz = new QuizMaterial(_materialId, _timeLimitMinutes, _passingScore, _showAnswers);

            var order = 1;
            foreach (var (optionCount, correctIndexes) in _questions)
            {
                var question = new Question(
                    _materialId, $"Câu hỏi {order}", QuestionType.SingleChoice, order);

                for (var i = 0; i < optionCount; i++)
                    question.AddAnswerOption(
                        new AnswerOption(question.Id, $"Đáp án {i + 1}", correctIndexes.Contains(i), i + 1));

                quiz.AddQuestion(question);
                order++;
            }

            return quiz;
        }
    }
}
