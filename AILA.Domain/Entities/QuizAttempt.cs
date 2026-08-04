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
        /// <summary>
        /// Dung sai cho độ trễ mạng và lệch đồng hồ máy khách: bài nộp tới trong khoảng này
        /// sau hạn chót vẫn được chấm (đánh dấu là tự động nộp), quá khoảng này thì bị từ chối.
        /// </summary>
        public const int SubmissionGraceSeconds = 30;

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

        /// <summary>
        /// Hạn chót do server quyết định, tính từ thời điểm bắt đầu lượt làm bài.
        /// Máy khách không được phép quyết định mốc này (AF-01).
        /// </summary>
        public DateTime GetDeadline(int timeLimitMinutes) => StartedAt.AddMinutes(timeLimitMinutes);

        /// <summary>Đã quá hạn chót (chưa tính dung sai) — dùng để đánh dấu "tự động nộp".</summary>
        public bool IsOverdue(int timeLimitMinutes, DateTime utcNow)
            => utcNow > GetDeadline(timeLimitMinutes);

        /// <summary>
        /// Đã quá hạn chót kể cả sau khi trừ dung sai — bài nộp tới lúc này phải bị từ chối.
        /// </summary>
        public bool IsPastGracePeriod(int timeLimitMinutes, DateTime utcNow)
            => utcNow > GetDeadline(timeLimitMinutes).AddSeconds(SubmissionGraceSeconds);

        /// <summary>
        /// Đóng lượt làm bài đã hết giờ mà không có bài nộp hợp lệ (AF-01): lượt không còn
        /// treo ở InProgress, không sinh kết quả giả và không đụng tới tiến độ khóa học.
        /// Learner muốn làm lại thì mở lượt mới qua endpoint start.
        /// </summary>
        public void Expire()
        {
            if (Status != QuizAttemptStatus.InProgress)
                throw new InvalidOperationException("Chỉ có thể đóng lượt làm bài đang dở dang.");

            Score = 0;
            IsPassed = false;
            Status = QuizAttemptStatus.Expired;

            UpdateTimestamp();
        }

        public void Submit(decimal score, bool isPassed)
        {
            if (Status == QuizAttemptStatus.Submitted)
                throw new InvalidOperationException("Bài kiểm tra đã được nộp.");

            if (Status == QuizAttemptStatus.Expired)
                throw new InvalidOperationException("Bài kiểm tra đã hết giờ, không thể nộp.");

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
