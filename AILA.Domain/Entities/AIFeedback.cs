using AILA.Domain.Common;
using System;

namespace AILA.Domain.Entities
{
    public class AIFeedback : BaseEntity
    {
        public Guid AttemptId { get; private set; }
        public decimal FinalScore { get; private set; }
        public string OverallSuggestion { get; private set; } = string.Empty;
        public string Strengths { get; private set; } = string.Empty;
        public string AreasForImprovement { get; private set; } = string.Empty;
        public string DetailedScoringJson { get; private set; } = string.Empty;
        public DateTime GradedAt { get; private set; } = DateTime.UtcNow;

        // Navigation property
        public virtual PracticeAttempt Attempt { get; private set; } = null!;

        // EF Core constructor
        private AIFeedback() { }

        public AIFeedback(
            Guid attemptId,
            decimal finalScore,
            string overallSuggestion,
            string strengths = "",
            string areasForImprovement = "",
            string detailedScoringJson = "")
        {
            if (attemptId == Guid.Empty)
                throw new ArgumentException("AttemptId không được để trống.", nameof(attemptId));

            Id = Guid.NewGuid();
            AttemptId = attemptId;
            FinalScore = finalScore;
            OverallSuggestion = overallSuggestion ?? string.Empty;
            Strengths = strengths ?? string.Empty;
            AreasForImprovement = areasForImprovement ?? string.Empty;
            DetailedScoringJson = detailedScoringJson ?? string.Empty;
            GradedAt = DateTime.UtcNow;
        }
    }
}
