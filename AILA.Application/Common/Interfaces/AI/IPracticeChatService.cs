using AILA.Application.Common.Dtos.AI;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AILA.Application.Common.Interfaces.AI;

public interface IPracticeChatService
{
    /// <summary>
    /// Gọi LLM với system prompt, user prompt, và conversation history (multi-turn).
    /// conversationHistory chứa các cặp user/assistant từ các turn trước.
    /// </summary>
    Task<string> GetChatResponseAsync(
        string systemPrompt,
        string userPrompt,
        List<ChatMessage>? conversationHistory = null,
        Guid? attemptId = null,
        Guid? accountId = null,
        CancellationToken cancellationToken = default);
}
