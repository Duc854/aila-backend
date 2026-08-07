using AILA.Application.Features.ExpertEvaluations.Dtos;
using AILA.Domain.Entities;

namespace AILA.Application.Features.ExpertEvaluations.Mapping
{
    /// <summary>
    /// Gom phần ánh xạ dùng chung giữa màn hình học viên (UC-30) và màn hình chuyên gia (UC-63).
    /// Mọi tham số đều nhận null để trường hợp học liệu đã bị xóa vẫn suy biến rõ ràng
    /// thay vì ném lỗi 500 (E63-4).
    /// </summary>
    public static class ExpertEvaluationMapper
    {
        public static PracticeAttemptContextDto ToAttemptContext(
            PracticeAttempt? attempt,
            Material? material,
            AIPracticeMaterial? practiceDetail)
        {
            return new PracticeAttemptContextDto
            {
                AttemptId = attempt?.Id ?? Guid.Empty,
                MaterialId = attempt?.MaterialId ?? Guid.Empty,
                MaterialTitle = material?.Title ?? string.Empty,
                Scenario = practiceDetail?.Scenario ?? string.Empty,
                LearnerTask = practiceDetail?.LearnerTask ?? string.Empty,
                AiTask = practiceDetail?.AITask ?? string.Empty,
                Difficulty = practiceDetail?.Difficulty.ToString(),
                Status = attempt?.Status.ToString() ?? string.Empty,
                CompletedAt = attempt?.CompletedAt,
                ScoringCriteria = practiceDetail?.ScoringCriterias
                    .OrderByDescending(c => c.Weight)
                    .Select(c => new ScoringCriterionDto
                    {
                        Id = c.Id,
                        Title = c.Title,
                        Description = c.Description,
                        Weight = c.Weight
                    })
                    .ToList() ?? new List<ScoringCriterionDto>()
            };
        }

        public static List<ConversationTurnDto> ToConversation(PracticeAttempt? attempt)
        {
            if (attempt is null)
                return new List<ConversationTurnDto>();

            return attempt.Submissions
                .OrderBy(s => s.CreatedAt)
                .Select(s => new ConversationTurnDto
                {
                    Id = s.Id,
                    UserPrompt = s.UserPrompt,
                    AiResponse = s.AiResponse,
                    SuggestedPrompt = string.IsNullOrWhiteSpace(s.SuggestedPrompt)
                        ? null
                        : s.SuggestedPrompt,
                    IsRejected = s.IsRejected,
                    RejectionReason = s.RejectionReason,
                    CreatedAt = s.CreatedAt
                })
                .ToList();
        }

        public static AiEvaluationDto ToAiEvaluation(PracticeAttempt? attempt)
        {
            return new AiEvaluationDto
            {
                FinalScore = attempt?.FinalScore,
                OverallSuggestion = attempt?.OverallSuggestion ?? string.Empty,
                EvaluatedAt = attempt?.CompletedAt
            };
        }

        public static ExpertEvaluationResultDto? ToExpertResult(ExpertEvaluation? evaluation)
        {
            if (evaluation is null)
                return null;

            return new ExpertEvaluationResultDto
            {
                Id = evaluation.Id,
                OverallScore = evaluation.OverallScore,
                Feedback = evaluation.Feedback,
                // Cột DB không cho null nên chuỗi rỗng nghĩa là chuyên gia không đưa khuyến nghị
                Recommendation = string.IsNullOrWhiteSpace(evaluation.Recommendation)
                    ? null
                    : evaluation.Recommendation,
                EvaluatedAt = evaluation.EvaluatedAt
            };
        }
    }
}
