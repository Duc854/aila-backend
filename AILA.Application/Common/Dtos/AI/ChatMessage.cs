namespace AILA.Application.Common.Dtos.AI;

/// <summary>Represents a single message in a conversation (for multi-turn chat history)</summary>
public record ChatMessage(string Role, string Content);
// Role = "user" (learner) hoặc "assistant" (AI persona)
