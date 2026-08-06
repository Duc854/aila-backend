using AILA.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Domain.Entities
{
    public class ExpertEvaluation : BaseEntity
    {
        public Guid EvaluationRequestId { get; private set; }

        public decimal OverallScore { get; private set; }

        public string Feedback { get; private set; }

        public string Recommendation { get; private set; }

        public DateTime EvaluatedAt { get; private set; }

        public virtual ExpertEvaluationRequest EvaluationRequest { get; private set; } = null!;

        private ExpertEvaluation() { }

        public ExpertEvaluation(
            Guid evaluationRequestId,
            decimal overallScore,
            string feedback,
            string recommendation)
        {
            EvaluationRequestId = evaluationRequestId;
            OverallScore = overallScore;
            Feedback = feedback;
            Recommendation = recommendation;

            EvaluatedAt = DateTime.UtcNow;
        }

        public void UpdateEvaluation(
            decimal overallScore,
            string feedback,
            string recommendation)
        {
            OverallScore = overallScore;
            Feedback = feedback;
            Recommendation = recommendation;

            UpdateTimestamp();
        }
    }
}
