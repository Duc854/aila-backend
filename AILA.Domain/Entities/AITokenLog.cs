namespace AILA.Domain.Entities;

using AILA.Domain.Common;
using System;

public class AITokenLog : BaseEntity
{
    public Guid AccountId { get; private set; }
    public Guid? AttemptId { get; private set; }
    public string ServiceType { get; private set; } = string.Empty;
    public string ModelId { get; private set; } = string.Empty;
    
    public int PromptTokens { get; private set; }
    public int CompletionTokens { get; private set; }
    public int TotalTokens { get; private set; }

    private AITokenLog() { }

    public AITokenLog(
        Guid accountId, 
        Guid? attemptId, 
        string serviceType, 
        string modelId, 
        int promptTokens, 
        int completionTokens)
    {
        Id = Guid.NewGuid();
        AccountId = accountId;
        AttemptId = attemptId;
        ServiceType = serviceType ?? string.Empty;
        ModelId = modelId ?? string.Empty;
        PromptTokens = promptTokens;
        CompletionTokens = completionTokens;
        TotalTokens = promptTokens + completionTokens;
    }
}
