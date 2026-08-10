using System;

namespace AILA.Application.Common.Dtos.AI;

public class AITokenLogDto
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public Guid? AttemptId { get; set; }
    public string ServiceType { get; set; } = string.Empty;
    public string ModelId { get; set; } = string.Empty;
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens { get; set; }
    public DateTime CreatedAt { get; set; }
}
