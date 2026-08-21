using AILA.Application.Common.Dtos.Rag;
using AILA.Application.Common.Interfaces.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AILA.Infrastructure.Services.AI;

public class RagChatService : IRagChatService
{
    private readonly ISessionValidator _sessionValidator;
    private readonly IVectorSearchService _vectorSearch;
    private readonly IPromptBuilder _promptBuilder;
    private readonly IChatResponseHandler _responseHandler;
    private readonly IMessagePersistence _messagePersistence;
    private readonly IPrivacyService _privacyService;
    private readonly IModerationService _moderationService;
    private readonly IQuotaService _quotaService;
    private readonly ILogger<RagChatService> _logger;
    private readonly RagChatConfig _config;

    public RagChatService(
        ISessionValidator sessionValidator,
        IVectorSearchService vectorSearch,
        IPromptBuilder promptBuilder,
        IChatResponseHandler responseHandler,
        IMessagePersistence messagePersistence,
        IPrivacyService privacyService,
        IModerationService moderationService,
        IQuotaService quotaService,
        ILogger<RagChatService> logger,
        IOptions<RagChatConfig> config)
    {
        _sessionValidator = sessionValidator;
        _vectorSearch = vectorSearch;
        _promptBuilder = promptBuilder;
        _responseHandler = responseHandler;
        _messagePersistence = messagePersistence;
        _privacyService = privacyService;
        _moderationService = moderationService;
        _quotaService = quotaService;
        _logger = logger;
        _config = config.Value;
    }

    public async Task<AskRagQuestionResponseDto> AskCourseQuestionAsync(
        Guid sessionId,
        Guid accountId,
        string question,
        CancellationToken cancellationToken = default)
    {
        // 1. Validate input
        if (string.IsNullOrWhiteSpace(question) || question.Trim().Length < 2)
        {
            return new AskRagQuestionResponseDto
            {
                MessageId = Guid.Empty,
                Question = question ?? string.Empty,
                Answer = string.Empty,
                Status = "ValidationError",
                IsViolation = false,
                WarningMessage = "Câu hỏi quá ngắn hoặc không có nội dung."
            };
        }

        var trimmedQuestion = question.Trim();

        // 2. PII Validation - Gọi service đã có
        var sanitizedQuestion = _privacyService.MaskSensitiveData(trimmedQuestion);
        if (_privacyService.HasSensitiveData(trimmedQuestion))
        {
            var piiTypes = _privacyService.GetSensitiveDataTypes(trimmedQuestion);
            var reason = $"Phát hiện thông tin cá nhân ({string.Join(", ", piiTypes)}). Vui lòng nhập theo hướng: \"{sanitizedQuestion}\"";

            await _messagePersistence.SaveViolationRecordAsync(
                accountId,
                "PromptValidationViolation",
                "PIIViolation",
                reason,
                sanitizedQuestion,
                cancellationToken);

            return new AskRagQuestionResponseDto
            {
                MessageId = Guid.NewGuid(),
                Question = sanitizedQuestion,
                Answer = string.Empty,
                Status = "Violation",
                IsViolation = true,
                ViolationMessage = reason,
                WarningMessage = reason
            };
        }

        // 3. Content Moderation - Gọi service đã có
        var (isSafe, moderationReason) = await _moderationService.CheckContentSafetyAsync(sanitizedQuestion, cancellationToken);
        if (!isSafe)
        {
            await _messagePersistence.SaveViolationRecordAsync(
                accountId,
                "ContentModerationViolation",
                "ContentModeration",
                moderationReason ?? "Vi phạm quy chuẩn an toàn nội dung",
                sanitizedQuestion,
                cancellationToken);

            return new AskRagQuestionResponseDto
            {
                MessageId = Guid.NewGuid(),
                Question = sanitizedQuestion,
                Answer = string.Empty,
                Status = "Violation",
                IsViolation = true,
                ViolationMessage = moderationReason ?? "Câu hỏi vi phạm quy chuẩn an toàn nội dung.",
                WarningMessage = moderationReason
            };
        }

        // 4. Quota Check - Gọi service đã có
        var quotaCheck = await _quotaService.CheckQuotaAsync(
            accountId,
            _config.QuotaLimit,
            _config.QuotaWarningThreshold,
            cancellationToken);

        if (!quotaCheck.IsAllowed)
        {
            return new AskRagQuestionResponseDto
            {
                MessageId = Guid.Empty,
                Question = sanitizedQuestion,
                Answer = string.Empty,
                Status = "QuotaExceeded",
                IsViolation = false,
                WarningMessage = quotaCheck.WarningMessage
            };
        }

        // 5. Session Validation - Tách sang service
        var sessionResult = await _sessionValidator.ValidateAsync(
            sessionId,
            accountId,
            sanitizedQuestion,
            _config.MaxHistoryMessages,
            cancellationToken);

        if (!sessionResult.IsValid)
        {
            return new AskRagQuestionResponseDto
            {
                MessageId = Guid.Empty,
                Question = sanitizedQuestion,
                Answer = string.Empty,
                Status = sessionResult.Status ?? "Forbidden",
                IsViolation = false,
                WarningMessage = sessionResult.ErrorMessage
            };
        }

        // 6. Vector Search - Tách sang service
        var searchResult = await _vectorSearch.SearchAsync(
            sessionResult.Session!.CourseId,
            sanitizedQuestion,
            _config.TopK,
            _config.MinSimilarity,
            cancellationToken);

        // 7. Build Prompt - Tách sang service
        var chatHistory = await _promptBuilder.BuildAsync(
            sanitizedQuestion,
            searchResult,
            sessionResult.RecentMessages,
            cancellationToken);

        // 8. Get LLM Response - Tách sang service
        var responseResult = await _responseHandler.GetResponseAsync(chatHistory, cancellationToken);

        // 9. Save Messages - Tách sang service
        await _messagePersistence.SaveMessagesAsync(
            sessionResult.Session.Id,
            sanitizedQuestion,
            responseResult.Answer,
            searchResult.Citations,
            responseResult.PromptTokens,
            responseResult.CompletionTokens,
            cancellationToken);

        // 10. Record Token Usage - Gọi service đã có
        await _quotaService.RecordTokenUsageAsync(
            accountId,
            sessionResult.Session.Id,
            "RagChat",
            responseResult.ModelId,
            responseResult.PromptTokens,
            responseResult.CompletionTokens,
            cancellationToken);

        return new AskRagQuestionResponseDto
        {
            MessageId = responseResult.MessageId,
            Question = sanitizedQuestion,
            Answer = responseResult.Answer,
            Citations = searchResult.Citations,
            Status = "Success",
            IsViolation = false,
            WarningMessage = quotaCheck.WarningMessage
        };
    }
}