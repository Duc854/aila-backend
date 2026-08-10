using AILA.Application.Common.Dtos.AI;
using AILA.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AILA.Application.Common.Interfaces.AI;

public record ScoringEvaluationResult(List<CriteriaScore> Scores, string SuggestedPrompt);

public interface IScoringService
{
    /// <summary>
    /// Chấm điểm 1 submission theo tất cả criteria (1 LLM call duy nhất, structured output).
    /// </summary>
    Task<ScoringEvaluationResult> EvaluateSubmissionAsync(
        Guid submissionId,
        string userPrompt,
        string aiResponse,
        List<ScoringCriteria> criteriaList,
        int retryLimit = 2,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Đánh giá tổng thể 1 lần duy nhất cho toàn bộ bài thực hành (AI Scorer)
    /// </summary>
    Task<OverallScoringResult> GenerateOverallSuggestionAsync(
        List<PromptSubmission> submissions,
        string scenario,
        string userTask,
        List<ScoringCriteria> criteriaList,
        string aiTask = "",
        Guid? attemptId = null,
        Guid? accountId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sinh câu thoại mẫu gợi ý (Suggested Prompt)
    /// </summary>
    Task<string> GeneratePromptSuggestionAsync(
        string userPrompt,
        string aiResponse,
        List<ScoringCriteria> criteriaList,
        CancellationToken cancellationToken = default);
}
