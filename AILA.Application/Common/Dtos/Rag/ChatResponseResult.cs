using System;

namespace AILA.Application.Common.Dtos.Rag;

public class ChatResponseResult
{
    public Guid MessageId { get; set; }
    public string Answer { get; set; } = string.Empty;
    public string ModelId { get; set; } = string.Empty;
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
}