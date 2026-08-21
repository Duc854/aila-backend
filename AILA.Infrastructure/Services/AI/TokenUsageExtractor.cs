using Microsoft.SemanticKernel;
using System;
using System.Text.Json;

namespace AILA.Infrastructure.Services.AI;

public static class TokenUsageExtractor
{
    public static (int PromptTokens, int CompletionTokens) Extract(
        ChatMessageContent? response,
        string? promptText = null,
        string? responseText = null,
        string? modelId = null)
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

        // Fallback ước tính theo kiến trúc Tokenizer của từng dòng Model nếu API không trả về Usage
        if (promptTokens <= 0 && !string.IsNullOrWhiteSpace(promptText))
        {
            var inputRatio = GetCharsPerTokenRatio(modelId, isInput: true);
            promptTokens = Math.Max(1, (int)Math.Ceiling(promptText.Length / inputRatio));
        }

        if (completionTokens <= 0 && !string.IsNullOrWhiteSpace(actualResponse))
        {
            var outputRatio = GetCharsPerTokenRatio(modelId, isInput: false);
            completionTokens = Math.Max(1, (int)Math.Ceiling(actualResponse.Length / outputRatio));
        }

        return (promptTokens, completionTokens);
    }

    /// <summary>
    /// Tính tỷ lệ ký tự / token dựa trên kiến trúc Tokenizer của từng dòng Model
    /// và sự khác biệt giữa Input (dày đặc cú pháp, Markdown, JSON, Tiếng Việt) và Output (văn bản tự nhiên).
    /// </summary>
    public static double GetCharsPerTokenRatio(string? modelId, bool isInput)
    {
        var model = modelId?.ToLowerInvariant().Trim() ?? string.Empty;

        // 1. Dòng OpenAI thế hệ mới nhất: GPT-5, GPT-5-mini, GPT-5.4-mini, GPT-4o, GPT-4o-mini, o1, o3 (Tokenizer o200k_base / vocab >= 200k)
        if (model.Contains("gpt-5") || model.Contains("gpt-4o") || model.Contains("gpt-4.5") || 
            model.Contains("o1") || model.Contains("o3") || model.Contains("chatgpt-4o"))
        {
            // Tiếng Việt & Markdown cú pháp: Input ~2.8 chars/token, Output ~3.2 chars/token
            return isInput ? 2.8 : 3.2;
        }

        // 2. Dòng Llama 3 / 3.1 / 3.2 / 3.3 (Groq / Meta - Vocab 128k)
        if (model.Contains("llama-3") || model.Contains("llama3"))
        {
            return isInput ? 2.6 : 3.0;
        }

        // 3. Dòng Google Gemini (Gemini 1.5 Flash/Pro, Gemini 2.0 - Vocab 256k)
        if (model.Contains("gemini"))
        {
            return isInput ? 3.0 : 3.4;
        }

        // 4. Dòng Anthropic Claude (Claude 3, Claude 3.5 Sonnet/Haiku)
        if (model.Contains("claude"))
        {
            return isInput ? 2.5 : 2.9;
        }

        // 5. Dòng OpenAI thế hệ cũ: GPT-3.5-Turbo, GPT-4 cũ (Tokenizer cl100k_base - Vocab 100k)
        if (model.Contains("gpt-3.5") || model.Contains("gpt-4"))
        {
            return isInput ? 2.3 : 2.7;
        }

        // Mặc định cho các model khác (tối ưu hóa ngữ liệu tiếng Việt)
        return isInput ? 2.5 : 3.0;
    }
}
