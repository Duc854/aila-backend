namespace AILA.Application.Features.ExpertEvaluations.Dtos
{
    /// <summary>Tiêu chí chấm điểm của kịch bản thực hành (UC-63 BR-02).</summary>
    public class ScoringCriterionDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Weight { get; set; }
    }

    /// <summary>Ngữ cảnh bài thực hành: kịch bản, nhiệm vụ và tiêu chí chấm.</summary>
    public class PracticeAttemptContextDto
    {
        public Guid AttemptId { get; set; }
        public Guid MaterialId { get; set; }
        public string MaterialTitle { get; set; } = string.Empty;
        public string Scenario { get; set; } = string.Empty;
        public string LearnerTask { get; set; } = string.Empty;
        public string AiTask { get; set; } = string.Empty;
        public string? Difficulty { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? CompletedAt { get; set; }
        public List<ScoringCriterionDto> ScoringCriteria { get; set; } = new();
    }

    /// <summary>Một lượt trao đổi giữa học viên và AI trong bài thực hành.</summary>
    public class ConversationTurnDto
    {
        public Guid Id { get; set; }
        public string UserPrompt { get; set; } = string.Empty;
        public string AiResponse { get; set; } = string.Empty;
        public string? SuggestedPrompt { get; set; }
        public bool IsRejected { get; set; }
        public string? RejectionReason { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>Kết quả chấm của AI, hiển thị song song với kết quả chuyên gia (BR-01).</summary>
    public class AiEvaluationDto
    {
        public decimal? FinalScore { get; set; }
        public string OverallSuggestion { get; set; } = string.Empty;
        public DateTime? EvaluatedAt { get; set; }
    }

    /// <summary>Kết quả chấm của chuyên gia. Read-only sau khi đã nộp (BR-03).</summary>
    public class ExpertEvaluationResultDto
    {
        public Guid Id { get; set; }
        public decimal OverallScore { get; set; }
        public string Feedback { get; set; } = string.Empty;
        public string? Recommendation { get; set; }
        public DateTime EvaluatedAt { get; set; }
    }

    /// <summary>UC-29: kết quả tạo yêu cầu đánh giá.</summary>
    public class ExpertEvaluationRequestCreatedDto
    {
        public Guid RequestId { get; set; }
        public Guid PracticeAttemptId { get; set; }
        public Guid? ExpertId { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime RequestedAt { get; set; }
        public int RemainingQuota { get; set; }
    }

    /// <summary>UC-30: màn hình học viên xem lại kết quả AI + chuyên gia.</summary>
    public class LearnerExpertEvaluationDto
    {
        public Guid RequestId { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime RequestedAt { get; set; }
        public DateTime? AssignedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        public PracticeAttemptContextDto Attempt { get; set; } = new();
        public List<ConversationTurnDto> Conversation { get; set; } = new();
        public AiEvaluationDto AiEvaluation { get; set; } = new();

        /// <summary>Chỉ có giá trị khi yêu cầu đã hoàn tất (AC-30.2).</summary>
        public ExpertEvaluationResultDto? ExpertEvaluation { get; set; }
    }

    /// <summary>UC-63: một dòng trong hàng chờ của chuyên gia.</summary>
    public class ExpertEvaluationRequestSummaryDto
    {
        public Guid RequestId { get; set; }
        public Guid PracticeAttemptId { get; set; }
        public Guid LearnerId { get; set; }
        public string LearnerName { get; set; } = string.Empty;
        public string MaterialTitle { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime RequestedAt { get; set; }
        public DateTime? AssignedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    /// <summary>UC-63: chi tiết một yêu cầu, đủ ngữ cảnh để chuyên gia chấm (BR-02).</summary>
    public class ExpertEvaluationRequestDetailDto
    {
        public Guid RequestId { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime RequestedAt { get; set; }
        public DateTime? AssignedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        public Guid LearnerId { get; set; }
        public string LearnerName { get; set; } = string.Empty;

        public PracticeAttemptContextDto Attempt { get; set; } = new();
        public List<ConversationTurnDto> Conversation { get; set; } = new();
        public AiEvaluationDto AiEvaluation { get; set; } = new();
        public ExpertEvaluationResultDto? ExpertEvaluation { get; set; }
    }
}
