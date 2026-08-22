using AILA.Application.Common.Dtos.Rag;
using AILA.Application.Common.Interfaces.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AILA.Infrastructure.Services.AI;

public class ChatResponseHandler : IChatResponseHandler
{
    private readonly IChatCompletionService _chatCompletion;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ChatResponseHandler> _logger;
    private readonly RagChatConfig _config;

    public ChatResponseHandler(
        IChatCompletionService chatCompletion,
        IConfiguration configuration,
        ILogger<ChatResponseHandler> logger,
        IOptions<RagChatConfig> config)
    {
        _chatCompletion = chatCompletion;
        _configuration = configuration;
        _logger = logger;
        _config = config.Value;
    }

    public async Task<ChatResponseResult> GetResponseAsync(
        ChatHistoryDto chatHistoryDto,
        CancellationToken cancellationToken = default)
    {
        // Convert ChatHistoryDto -> SemanticKernel ChatHistory
        var chatHistory = new ChatHistory();
        foreach (var msg in chatHistoryDto.Messages)
        {
            if (msg.Role.Equals("system", StringComparison.OrdinalIgnoreCase))
                chatHistory.AddSystemMessage(msg.Content);
            else if (msg.Role.Equals("assistant", StringComparison.OrdinalIgnoreCase))
                chatHistory.AddAssistantMessage(msg.Content);
            else
                chatHistory.AddUserMessage(msg.Content);
        }

        // Lấy từ RagChatService cũ
        var executionSettings = new OpenAIPromptExecutionSettings
        {
            Temperature = _config.Temperature
        };

        // Lấy từ RagChatService cũ
        ChatMessageContent? response = null;
        var modelId = _configuration["OpenAI:ModelId"] ?? _config.DefaultModelId;

        // Lấy từ RagChatService cũ (có cải tiến exponential backoff)
        for (int attempt = 0; attempt < _config.MaxRetries; attempt++)
        {
            try
            {
                response = await _chatCompletion.GetChatMessageContentAsync(
                    chatHistory,
                    executionSettings,
                    cancellationToken: cancellationToken);
                break;
            }
            catch (Exception ex) when (attempt < _config.MaxRetries - 1 && IsRateLimitError(ex))
            {
                var delay = _config.RetryBaseDelaySeconds * Math.Pow(2, attempt);
                _logger.LogWarning(ex,
                    "Rate limit hit (attempt {Attempt}/{MaxRetries}), retrying in {Delay}s",
                    attempt + 1, _config.MaxRetries, delay);

                await Task.Delay(TimeSpan.FromSeconds(delay), cancellationToken);
            }
        }

        if (response == null)
        {
            throw new InvalidOperationException("Failed to get response from LLM after retries");
        }

        // Lấy từ RagChatService cũ
        var answer = response.Content ?? "Xin lỗi, đã xảy ra lỗi khi xử lý câu hỏi của bạn.";
        var promptText = string.Join("\n", chatHistory.Select(m => m.Content));

        // Lấy từ RagChatService cũ
        var (promptTokens, completionTokens) = TokenUsageExtractor.Extract(
            response,
            promptText,
            answer,
            modelId);

        return new ChatResponseResult
        {
            MessageId = Guid.NewGuid(),
            Answer = answer,
            ModelId = modelId,
            PromptTokens = promptTokens,
            CompletionTokens = completionTokens
        };
    }

    private static bool IsRateLimitError(Exception ex)
    {
        var message = ex.Message.ToLowerInvariant();
        return message.Contains("429") ||
               message.Contains("rate limit") ||
               message.Contains("too many requests");
    }
}