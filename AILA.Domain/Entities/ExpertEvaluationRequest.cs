using AILA.Domain.Common;
using AILA.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Domain.Entities
{
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
            PracticeAttemptId = practiceAttemptId;
            LearnerId = learnerId;

            Status = ExpertEvaluationRequestStatus.Pending;
            RequestedAt = DateTime.UtcNow;
        }

        public void AssignExpert(Guid expertId)
        {
            ExpertId = expertId;
            AssignedAt = DateTime.UtcNow;
            Status = ExpertEvaluationRequestStatus.InProgress;

            UpdateTimestamp();
        }

        public void Complete()
        {
            Status = ExpertEvaluationRequestStatus.Completed;
            CompletedAt = DateTime.UtcNow;

            UpdateTimestamp();
        }

        public void Cancel()
        {
            Status = ExpertEvaluationRequestStatus.Cancelled;
            UpdateTimestamp();
        }
    }
}
