using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Domain.Entities
{
    public class QuizMaterial
    {
        private readonly List<Question> _questions = new();

        public Guid MaterialId { get; private set; }

        public int TimeLimitMinutes { get; private set; }

        public decimal PassingScore { get; private set; }

        public bool ShowCorrectAnswersAfterSubmission { get; private set; }

        // Navigation Property
        public virtual Material Material { get; private set; } = null!;

        public virtual IReadOnlyCollection<Question> Questions => _questions.AsReadOnly();

        private readonly List<QuizAttempt> _quizAttempts = new();

        public virtual IReadOnlyCollection<QuizAttempt> QuizAttempts
            => _quizAttempts.AsReadOnly();

        private QuizMaterial() { }

        public QuizMaterial(
            Guid materialId,
            int timeLimitMinutes,
            decimal passingScore,
            bool showCorrectAnswersAfterSubmission)
        {
            if (materialId == Guid.Empty)
                throw new ArgumentException("Mã học liệu không hợp lệ.", nameof(materialId));

            if (timeLimitMinutes <= 0)
                throw new ArgumentException("Thời gian làm bài phải lớn hơn 0 phút.", nameof(timeLimitMinutes));

            if (passingScore < 0 || passingScore > 100)
                throw new ArgumentException("Điểm đạt phải nằm trong khoảng từ 0 đến 100.", nameof(passingScore));

            MaterialId = materialId;
            TimeLimitMinutes = timeLimitMinutes;
            PassingScore = passingScore;
            ShowCorrectAnswersAfterSubmission = showCorrectAnswersAfterSubmission;
        }

        public void UpdateSetting(
            int timeLimitMinutes,
            decimal passingScore,
            bool showCorrectAnswersAfterSubmission)
        {
            if (timeLimitMinutes <= 0)
                throw new ArgumentException("Thời gian làm bài phải lớn hơn 0 phút.", nameof(timeLimitMinutes));

            if (passingScore < 0 || passingScore > 100)
                throw new ArgumentException("Điểm đạt phải nằm trong khoảng từ 0 đến 100.", nameof(passingScore));

            TimeLimitMinutes = timeLimitMinutes;
            PassingScore = passingScore;
            ShowCorrectAnswersAfterSubmission = showCorrectAnswersAfterSubmission;
        }

        public void AddQuestion(Question question)
        {
            ArgumentNullException.ThrowIfNull(question);

            if (_questions.Any(q => q.Id == question.Id))
                throw new InvalidOperationException("Câu hỏi đã tồn tại trong bài kiểm tra.");

            _questions.Add(question);
        }

        public void RemoveQuestion(Guid questionId)
        {
            var question = _questions.FirstOrDefault(q => q.Id == questionId)
                ?? throw new ArgumentException("Không tìm thấy câu hỏi.", nameof(questionId));

            _questions.Remove(question);
        }
    }
}
