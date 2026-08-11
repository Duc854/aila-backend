using Microsoft.SemanticKernel;
using System;
using System.Text.Json;

namespace AILA.Infrastructure.Services.AI;

public static class TokenUsageExtractor
{
    public static (int PromptTokens, int CompletionTokens) Extract(
        ChatMessageContent? response,
        string? promptText = null,
        string? responseText = null)
    {
        int promptTokens = 0;
        int completionTokens = 0;

        if (response?.Metadata != null)
        {
            // Semantic Kernel / OpenAI .NET SDK Usage metadata keys
            object? usageObj = null;
            if (response.Metadata.TryGetValue("Usage", out var u1)) usageObj = u1;
            else if (response.Metadata.TryGetValue("UsageReport", out var u2)) usageObj = u2;
            else if (response.Metadata.TryGetValue("usage", out var u3)) usageObj = u3;
            else if (response.Metadata.TryGetValue("ChatTokenUsage", out var u4)) usageObj = u4;

            if (usageObj != null)
            {
                try
                {
                    var usageJson = JsonSerializer.Serialize(usageObj);
                    using var usageDoc = JsonDocument.Parse(usageJson);
                    var root = usageDoc.RootElement;

                    // 1. Input / Prompt Tokens (Hỗ trợ tất cả format của OpenAI .NET v2, v1, Groq, REST API)
                    if (root.TryGetProperty("InputTokenCount", out var e1) && e1.TryGetInt32(out var v1)) promptTokens = v1;
                    else if (root.TryGetProperty("InputTokens", out var e2) && e2.TryGetInt32(out var v2)) promptTokens = v2;
                    else if (root.TryGetProperty("PromptTokens", out var e3) && e3.TryGetInt32(out var v3)) promptTokens = v3;
                    else if (root.TryGetProperty("prompt_tokens", out var e4) && e4.TryGetInt32(out var v4)) promptTokens = v4;
                    else if (root.TryGetProperty("PromptTokenCount", out var e5) && e5.TryGetInt32(out var v5)) promptTokens = v5;

                    // 2. Output / Completion Tokens
                    if (root.TryGetProperty("OutputTokenCount", out var o1) && o1.TryGetInt32(out var vo1)) completionTokens = vo1;
                    else if (root.TryGetProperty("OutputTokens", out var o2) && o2.TryGetInt32(out var vo2)) completionTokens = vo2;
                    else if (root.TryGetProperty("CompletionTokens", out var o3) && o3.TryGetInt32(out var vo3)) completionTokens = vo3;
                    else if (root.TryGetProperty("completion_tokens", out var o4) && o4.TryGetInt32(out var vo4)) completionTokens = vo4;
                    else if (root.TryGetProperty("CompletionTokenCount", out var o5) && o5.TryGetInt32(out var vo5)) completionTokens = vo5;
                }
                catch { }
            }
        }

        var actualResponse = responseText ?? response?.Content ?? string.Empty;

        // Fallback ước tính chính xác (1 token ~ 3.5 ký tự) nếu API không trả về Usage
        if (promptTokens <= 0 && !string.IsNullOrWhiteSpace(promptText))
        {
            promptTokens = Math.Max(10, (int)Math.Ceiling(promptText.Length / 3.5));
        }

        if (completionTokens <= 0 && !string.IsNullOrWhiteSpace(actualResponse))
        {
            completionTokens = Math.Max(10, (int)Math.Ceiling(actualResponse.Length / 3.5));
        }

        return (promptTokens, completionTokens);
    }
}
