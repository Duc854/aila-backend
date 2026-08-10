namespace AILA.Domain.Entities;

using AILA.Domain.Common;
using System;

public class PromptSubmission : BaseEntity
{
    public Guid AttemptId { get; private set; }
    public string UserPrompt { get; private set; } = string.Empty;
    public string AiResponse { get; private set; } = string.Empty;

    // Navigation property
    public virtual PracticeAttempt Attempt { get; private set; } = null!;

    private PromptSubmission() { }

    public PromptSubmission(Guid attemptId, string userPrompt, string aiResponse)
    {
        Id = Guid.NewGuid();
        AttemptId = attemptId;
        UserPrompt = userPrompt;
        AiResponse = aiResponse;
    }

    public void SetAiResponse(string aiResponse)
    {
        AiResponse = aiResponse ?? string.Empty;
    }
}
