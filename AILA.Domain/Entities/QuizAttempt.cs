using AILA.Domain.Common;
using AILA.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Domain.Entities
{
    public class QuizAttempt : BaseEntity
    {
        private readonly List<QuizAnswer> _answers = new();

        public Guid EnrollmentId { get; private set; }

        public Guid QuizMaterialId { get; private set; }

        public DateTime StartedAt { get; private set; }

        public DateTime? SubmittedAt { get; private set; }

        public decimal Score { get; private set; }

        public bool IsPassed { get; private set; }

        public QuizAttemptStatus Status { get; private set; }

        // Navigation
        public virtual Enrollment Enrollment { get; private set; } = null!;

        public virtual QuizMaterial QuizMaterial { get; private set; } = null!;

        public virtual IReadOnlyCollection<QuizAnswer> Answers => _answers.AsReadOnly();

        private QuizAttempt() { }

        public QuizAttempt(
            Guid enrollmentId,
            Guid quizMaterialId)
        {
            if (enrollmentId == Guid.Empty)
                throw new ArgumentException("Mã đăng ký khóa học không hợp lệ.", nameof(enrollmentId));

            if (quizMaterialId == Guid.Empty)
                throw new ArgumentException("Mã bài kiểm tra không hợp lệ.", nameof(quizMaterialId));

            Id = Guid.NewGuid();
            EnrollmentId = enrollmentId;
            QuizMaterialId = quizMaterialId;
            StartedAt = DateTime.UtcNow;
            Status = QuizAttemptStatus.InProgress;
        }

        public void AddAnswer(QuizAnswer answer)
        {
            ArgumentNullException.ThrowIfNull(answer);

            if (Status != QuizAttemptStatus.InProgress)
                throw new InvalidOperationException("Không thể thêm câu trả lời khi bài kiểm tra đã nộp.");

            // Cho phép nhiều đáp án cho một câu hỏi (câu hỏi nhiều lựa chọn),
            // chỉ chặn ghi trùng cùng một lựa chọn cho cùng một câu hỏi.
            if (_answers.Any(x => x.QuestionId == answer.QuestionId
                                  && x.SelectedAnswerOptionId == answer.SelectedAnswerOptionId))
                throw new InvalidOperationException("Lựa chọn này đã được ghi nhận cho câu hỏi.");

            _answers.Add(answer);

            UpdateTimestamp();
        }

        public void RemoveAnswer(Guid questionId)
        {
            var answer = _answers.FirstOrDefault(x => x.QuestionId == questionId);

            if (answer == null)
                return;

            _answers.Remove(answer);

            UpdateTimestamp();
        }

        public void Submit(decimal score, bool isPassed)
        {
            if (Status == QuizAttemptStatus.Submitted)
                throw new InvalidOperationException("Bài kiểm tra đã được nộp.");

            if (score < 0 || score > 100)
                throw new ArgumentException("Điểm số phải nằm trong khoảng từ 0 đến 100.", nameof(score));

            Score = score;
            IsPassed = isPassed;
            SubmittedAt = DateTime.UtcNow;
            Status = QuizAttemptStatus.Submitted;

            UpdateTimestamp();
        }
    }
}
