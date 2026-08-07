namespace AILA.Domain.Entities;

using AILA.Domain.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

public class PromptSubmission : BaseEntity
{
    public Guid AttemptId { get; private set; }
    public string UserPrompt { get; private set; } = string.Empty;
    public string AiResponse { get; private set; } = string.Empty;
    
    /// <summary>Câu thoại mẫu chuẩn do AI đề xuất giúp người học đạt điểm tối đa</summary>
    public string SuggestedPrompt { get; private set; } = string.Empty;
    
    public bool IsRejected { get; private set; }
    public string? RejectionReason { get; private set; }
    public string? PolicyName { get; private set; }

    [ForeignKey("SubmissionId")]
    public List<CriteriaScore> CriteriaScores { get; private set; } = new();

    private PromptSubmission() { }

    public PromptSubmission(Guid attemptId, string userPrompt, string aiResponse)
    {
        Id = Guid.NewGuid();
        AttemptId = attemptId;
        UserPrompt = userPrompt;
        AiResponse = aiResponse;
        IsRejected = false;
    }

    public void SetSuggestedPrompt(string suggestedPrompt)
    {
        SuggestedPrompt = suggestedPrompt ?? string.Empty;
    }

    public void SetAiResponse(string aiResponse)
    {
        AiResponse = aiResponse ?? string.Empty;
    }

    public void Reject(string reason, string? policyName = null)
    {
        IsRejected = true;
        RejectionReason = reason;
        PolicyName = policyName;
        UpdateTimestamp();
    }
}
