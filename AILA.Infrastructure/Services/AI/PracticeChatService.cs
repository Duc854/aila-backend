using AILA.Application.Common.Interfaces;
using AILA.Application.Common.Dtos.AI;
using AILA.Application.Common.Interfaces.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AILA.Infrastructure.Services.AI;

public class PracticeChatService : IPracticeChatService
{
    private readonly IChatCompletionService _chatCompletion;
    private readonly IConfiguration _configuration;
    private readonly IRoleParserService _roleParser;
    private readonly IQuotaService _quotaService;

    public PracticeChatService(
        IChatCompletionService chatCompletion,
        IConfiguration configuration,
        IRoleParserService roleParser,
        IQuotaService quotaService)
    {
        _chatCompletion = chatCompletion;
        _configuration = configuration;
        _roleParser = roleParser;
        _quotaService = quotaService;
    }

    public async Task<string> GetChatResponseAsync(
        string systemPrompt,
        string userPrompt,
        List<ChatMessage>? conversationHistory = null,
        Guid? attemptId = null,
        Guid? accountId = null,
        CancellationToken cancellationToken = default)
    {
        var modelId = _configuration["OpenAI:ModelId"] ?? "llama-3.1-8b-instant";

        var roleResult = await _roleParser.ParseRolesAsync(systemPrompt, cancellationToken);
        var roleBoundary = roleResult.IsSuccess
            ? $"\n\n==================================================\n" +
              $"[RẤT QUAN TRỌNG - CHÍNH SÁCH BẢO VỆ XƯNG HỒ & VAI DIỄN]:\n" +
              $"1. VAI TRÒ BẮT BUỘC CỦA BẠN (AI): '{roleResult.AIRole}'.\n" +
              $"2. VAI TRÒ CỦA NGUỜI CHAT (HỌC VIÊN): '{roleResult.UserRole}'.\n" +
              $"3. NẾU AI LÀ MENTOR/CHUYÊN GIA/BA/SENIOR CODER: AI BẮT BUỘC xưng 'Anh' (hoặc 'Chị'/'Mentor') và gọi Học viên là 'em'. TUYỆT ĐỐI KHÔNG xưng 'em' hay chào 'Em chào anh'!\n" +
              $"4. NẾU AI LÀ SINH VIÊN/NGƯỜI XIN TƯ VẤN: AI BẮT BUỘC xưng 'em' và gọi Học viên là 'Anh/Chị/Mentor'.\n" +
              $"=================================================="
            : string.Empty;

        var finalSystemPrompt = AILA.Application.Common.Constants.SystemPromptConstants.PlatformSystemPrompt 
            + roleBoundary 
            + "\n\n" 
            + systemPrompt;

        Console.WriteLine("\n==================================================");
        Console.WriteLine("🔥 [FINAL SYSTEM PROMPT VIA SEMANTIC KERNEL]:");
        Console.WriteLine(finalSystemPrompt);
        Console.WriteLine("==================================================\n");

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(finalSystemPrompt);

        if (conversationHistory != null)
        {
            foreach (var msg in conversationHistory)
            {
                if (msg.Role.Equals("assistant", StringComparison.OrdinalIgnoreCase))
                {
                    chatHistory.AddAssistantMessage(msg.Content);
                }
                else
                {
                    chatHistory.AddUserMessage(msg.Content);
                }
            }
        }

        chatHistory.AddUserMessage(userPrompt);

        var executionSettings = new OpenAIPromptExecutionSettings
        {
            Temperature = 0.7
        };

        var response = await _chatCompletion.GetChatMessageContentAsync(
            chatHistory,
            executionSettings,
            cancellationToken: cancellationToken);

        int promptTokens = 0;
        int completionTokens = 0;

        if (response.Metadata != null && response.Metadata.TryGetValue("Usage", out var usageObj))
        {
            try
            {
                var usageJson = JsonSerializer.Serialize(usageObj);
                using var usageDoc = JsonDocument.Parse(usageJson);
                if (usageDoc.RootElement.TryGetProperty("InputTokens", out var pElem)) promptTokens = pElem.GetInt32();
                else if (usageDoc.RootElement.TryGetProperty("PromptTokens", out pElem)) promptTokens = pElem.GetInt32();
                else if (usageDoc.RootElement.TryGetProperty("prompt_tokens", out pElem)) promptTokens = pElem.GetInt32();

                if (usageDoc.RootElement.TryGetProperty("OutputTokens", out var cElem)) completionTokens = cElem.GetInt32();
                else if (usageDoc.RootElement.TryGetProperty("CompletionTokens", out cElem)) completionTokens = cElem.GetInt32();
                else if (usageDoc.RootElement.TryGetProperty("completion_tokens", out cElem)) completionTokens = cElem.GetInt32();
            }
            catch { }
        }

        Console.WriteLine($"🔥 [TOKEN USED - SEMANTIC KERNEL CHAT ROLEPLAY]: PromptTokens={promptTokens}, CompletionTokens={completionTokens}, TotalTokens={promptTokens + completionTokens}");

        var targetAccountId = accountId ?? Guid.Empty;

        if (targetAccountId != Guid.Empty)
        {
            await _quotaService.RecordTokenUsageAsync(
                targetAccountId,
                attemptId,
                "ChatRoleplay",
                modelId,
                promptTokens,
                completionTokens,
                cancellationToken);
        }

        return response.Content ?? string.Empty;
    }
}
