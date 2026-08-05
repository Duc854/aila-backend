using AILA.Domain.Common;
using System;

namespace AILA.Domain.Entities;

public class CourseChatMessage : BaseEntity
{
    public Guid SessionId { get; private set; }
    public string Role { get; private set; } = string.Empty; // "user" hoặc "assistant"
    public string Content { get; private set; } = string.Empty;
    public string? CitationsJson { get; private set; }
    public int PromptTokens { get; private set; }
    public int CompletionTokens { get; private set; }

    public virtual CourseChatSession Session { get; private set; } = null!;

    private CourseChatMessage() { }

    public CourseChatMessage(
        Guid sessionId,
        string role,
        string content,
        string? citationsJson = null,
        int promptTokens = 0,
        int completionTokens = 0)
    {
        Id = Guid.NewGuid();
        SessionId = sessionId;
        Role = role.ToLower().Trim();
        Content = content;
        CitationsJson = citationsJson;
        PromptTokens = promptTokens;
        CompletionTokens = completionTokens;
    }
}
