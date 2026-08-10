using AILA.Domain.Common;
using AILA.Domain.Enums;
using System;

namespace AILA.Domain.Entities
{
    /// <summary>
    /// Aggregate root của luồng nhờ chuyên gia đánh giá một lượt thực hành.
    /// Vòng đời: Pending -> InProgress -> Completed, có thể Cancelled khi chưa hoàn tất.
    /// </summary>
    public class ExpertEvaluationRequest : BaseEntity
    {
        public Guid PracticeAttemptId { get; private set; }
        public Guid LearnerId { get; private set; }
        public Guid? ExpertId { get; private set; }

        public ExpertEvaluationRequestStatus Status { get; private set; }

        public DateTime RequestedAt { get; private set; }
        public DateTime? AssignedAt { get; private set; }
        public DateTime? CompletedAt { get; private set; }

        public virtual PracticeAttempt PracticeAttempt { get; private set; } = null!;
        public virtual Learner Learner { get; private set; } = null!;
        public virtual Expert? Expert { get; private set; }

        public virtual ExpertEvaluation? ExpertEvaluation { get; private set; }

        private ExpertEvaluationRequest() { }

        public ExpertEvaluationRequest(Guid practiceAttemptId, Guid learnerId)
        {
            if (practiceAttemptId == Guid.Empty)
                throw new ArgumentException("Mã lượt thực hành không hợp lệ.", nameof(practiceAttemptId));

            if (learnerId == Guid.Empty)
                throw new ArgumentException("Mã học viên không hợp lệ.", nameof(learnerId));

            Id = Guid.NewGuid();
            PracticeAttemptId = practiceAttemptId;
            LearnerId = learnerId;

            Status = ExpertEvaluationRequestStatus.Pending;
            RequestedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Giao yêu cầu cho chuyên gia tác giả khóa học (BR-04). Chỉ hợp lệ khi đang chờ gán.
        /// </summary>
        public void AssignExpert(Guid expertId)
        {
            if (expertId == Guid.Empty)
                throw new ArgumentException("Mã chuyên gia không hợp lệ.", nameof(expertId));

            if (Status != ExpertEvaluationRequestStatus.Pending)
                throw new InvalidOperationException(
                    "Chỉ yêu cầu đang chờ mới được giao cho chuyên gia.");

            ExpertId = expertId;
            AssignedAt = DateTime.UtcNow;
            Status = ExpertEvaluationRequestStatus.InProgress;

            UpdateTimestamp();
        }

        /// <summary>
        /// Chốt yêu cầu sau khi chuyên gia đã nộp kết quả. Chỉ hợp lệ khi đang được xử lý.
        /// </summary>
        public void Complete()
        {
            if (Status != ExpertEvaluationRequestStatus.InProgress)
                throw new InvalidOperationException(
                    "Chỉ yêu cầu đang được chuyên gia xử lý mới được hoàn tất.");

            Status = ExpertEvaluationRequestStatus.Completed;
            CompletedAt = DateTime.UtcNow;

            UpdateTimestamp();
        }

        /// <summary>
        /// Hủy yêu cầu khi chưa hoàn tất. Kết quả đã chốt là bất biến nên không thể hủy.
        /// </summary>
        public void Cancel()
        {
            if (Status == ExpertEvaluationRequestStatus.Completed)
                throw new InvalidOperationException(
                    "Không thể hủy yêu cầu đã hoàn tất.");

            if (Status == ExpertEvaluationRequestStatus.Cancelled)
                return;

            Status = ExpertEvaluationRequestStatus.Cancelled;

            UpdateTimestamp();
        }
    }
}
