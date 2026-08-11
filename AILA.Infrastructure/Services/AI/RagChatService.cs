using AILA.Application.Common.Interfaces;
using AILA.Application.Common.Dtos.Rag;
using AILA.Application.Common.Interfaces.AI;
using AILA.Application.Common.Interfaces.Repositories;
using AILA.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AILA.Infrastructure.Services.AI;

public class RagChatService : IRagChatService
{
    private readonly IKnowledgeChunkRepository _repository;
    private readonly IKnowledgeBaseService _knowledgeBaseService;
    private readonly IQuotaService _quotaService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IChatCompletionService _chatCompletion;
    private readonly IConfiguration _configuration;
    private readonly IPrivacyService _privacyService;
    private readonly IModerationService _moderationService;

    public RagChatService(
        IKnowledgeChunkRepository repository,
        IKnowledgeBaseService knowledgeBaseService,
        IQuotaService quotaService,
        IUnitOfWork unitOfWork,
        IChatCompletionService chatCompletion,
        IConfiguration configuration,
        IPrivacyService privacyService,
        IModerationService moderationService)
    {
        _repository = repository;
        _knowledgeBaseService = knowledgeBaseService;
        _quotaService = quotaService;
        _unitOfWork = unitOfWork;
        _chatCompletion = chatCompletion;
        _configuration = configuration;
        _privacyService = privacyService;
        _moderationService = moderationService;
    }

    public async Task<AskRagQuestionResponseDto> AskCourseQuestionAsync(
        Guid sessionId,
        Guid accountId,
        string question,
        CancellationToken cancellationToken = default)
    {
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

        // 1. PII Masking & Privacy Violation Check (Chặn ngay lập tức thông tin cá nhân như số điện thoại, CCCD, Email...)
        var sanitizedQuestion = _privacyService.MaskSensitiveData(trimmedQuestion);
        if (_privacyService.HasSensitiveData(trimmedQuestion))
        {
            var piiTypes = _privacyService.GetSensitiveDataTypes(trimmedQuestion);
            var violationReason = $"Phát hiện thông tin cá nhân ({string.Join(", ", piiTypes)}). Vui lòng nhập theo hướng: \"{sanitizedQuestion}\"";

            // Lưu vết vi phạm vào UserViolationRecord
            var violationRecord = new UserViolationRecord(
                accountId,
                "PromptValidationViolation",
                "PIIViolation",
                violationReason,
                sanitizedQuestion);

            await _unitOfWork.Repository<UserViolationRecord>().AddAsync(violationRecord);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Chặn luôn, không gọi AI
            return new AskRagQuestionResponseDto
            {
                MessageId = Guid.NewGuid(),
                Question = sanitizedQuestion,
                Answer = string.Empty,
                Status = "Violation",
                IsViolation = true,
                ViolationMessage = violationReason,
                WarningMessage = violationReason
            };
        }

        // 2. Content Moderation Check (Kiểm tra độc hại/vi phạm an toàn nội dung)
        var (isSafe, moderationReason) = await _moderationService.CheckContentSafetyAsync(sanitizedQuestion, cancellationToken);
        if (!isSafe)
        {
            var violationRecord = new UserViolationRecord(
                accountId,
                "ContentModerationViolation",
                "ContentModeration",
                moderationReason ?? "Vi phạm quy chuẩn an toàn nội dung",
                sanitizedQuestion);

            await _unitOfWork.Repository<UserViolationRecord>().AddAsync(violationRecord);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

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

        // 3. Check Quota Limit
        var quotaCheck = await _quotaService.CheckQuotaAsync(accountId, 1000, 0.80f, cancellationToken);
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

        // 4. Fetch Chat Session
        var session = await _repository.GetSessionByIdAsync(sessionId, cancellationToken);
        if (session == null)
        {
            throw new InvalidOperationException($"Không tìm thấy phiên trò chuyện RAG ID: {sessionId}");
        }

        // 5. Generate Vector Embedding for user question & Retrieve top 3 chunks
        var queryEmbedding = await _knowledgeBaseService.GenerateEmbeddingAsync(sanitizedQuestion, cancellationToken);
        var similarChunks = await _repository.SearchSimilarChunksAsync(session.CourseId, queryEmbedding, topK: 3, cancellationToken);

        // 6. Build Citations List
        var citations = new List<RagCitationDto>();
        var contextTextBuilder = new System.Text.StringBuilder();

        for (int i = 0; i < similarChunks.Count; i++)
        {
            var (chunk, realScore) = similarChunks[i];
            string title = $"Bài học #{chunk.ChunkIndex}";
            if (!string.IsNullOrWhiteSpace(chunk.MetadataJson))
            {
                try
                {
                    using var metaDoc = JsonDocument.Parse(chunk.MetadataJson);
                    if (metaDoc.RootElement.TryGetProperty("MaterialTitle", out var tProp))
                    {
                        title = tProp.GetString() ?? title;
                    }
                }
                catch { }
            }

            citations.Add(new RagCitationDto
            {
                MaterialId = chunk.MaterialId,
                MaterialTitle = title,
                Snippet = chunk.Content.Length > 150 ? chunk.Content.Substring(0, 150) + "..." : chunk.Content,
                SimilarityScore = realScore > 0 ? realScore : 0.85
            });

            contextTextBuilder.AppendLine($"--- [Trích dẫn từ bài học: {title}] ---");
            contextTextBuilder.AppendLine(chunk.Content);
            contextTextBuilder.AppendLine();
        }

        // 7. Build RAG Prompt (Hybrid RAG: Course Material Priority + LLM General Knowledge Fallback)
        var systemInstruction = @"Bạn là trợ lý AI thông minh phụ trách giải đáp thắc mắc cho Học viên trong khóa học.
Dưới đây là NỘI DUNG TÀI LIỆU BÀI HỌC được trích xuất từ hệ thống:
" + contextTextBuilder.ToString() + @"
YÊU CẦU TRẢ LỜI:
1. ƯU TIÊN HÀNG ĐẦU: Nếu câu hỏi nằm trong NỘI DUNG TÀI LIỆU BÀI HỌC ở trên, hãy dùng kiến thức đó để giải đáp chính xác cho Học viên.
2. NẾU CÂU HỎI NẰM NGOÀI TÀI LIỆU BÀI HỌC: Hãy vận dụng kiến thức chuyên môn rộng lớn của bạn để giải đáp chi tiết, chu đáo và hữu ích cho Học viên (TUYỆT ĐỐI KHÔNG từ chối trả lời hoặc bảo 'tôi không biết').
3. Trả lời bằng tiếng Việt, mạch lạc, dễ hiểu, thái độ hỗ trợ nhiệt tình.";

        // 8. Fetch Recent Conversation History for Multi-turn Context via Semantic Kernel
        var previousMessages = await _repository.GetMessagesBySessionIdAsync(sessionId, cancellationToken);
        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(systemInstruction);

        var recentHistory = previousMessages.TakeLast(6).ToList();
        foreach (var msg in recentHistory)
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

        chatHistory.AddUserMessage(sanitizedQuestion);

        var executionSettings = new OpenAIPromptExecutionSettings
        {
            Temperature = 0.3
        };

        ChatMessageContent? response = null;
        int maxRetries = 3;
        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                response = await _chatCompletion.GetChatMessageContentAsync(
                    chatHistory,
                    executionSettings,
                    cancellationToken: cancellationToken);
                break;
            }
            catch (Exception ex) when (attempt < maxRetries - 1 && (ex.Message.Contains("429") || ex.Message.Contains("Rate limit")))
            {
                Console.WriteLine($"⚠️ [RagChatService Groq Rate Limit 429 via Semantic Kernel] Retrying in 4 seconds (Attempt {attempt + 1}/{maxRetries})...");
                await Task.Delay(4000, cancellationToken);
            }
        }

        string answer = response?.Content ?? "Xin lỗi, đã xảy ra lỗi khi xử lý câu hỏi của bạn.";
        string promptText = string.Join("\n", chatHistory.Select(m => m.Content));
        var (promptTokens, completionTokens) = TokenUsageExtractor.Extract(response, promptText, answer);

        var modelId = _configuration["OpenAI:ModelId"] ?? "llama-3.1-8b-instant";

        // 9. Record Token Usage into AITokenLogs
        await _quotaService.RecordTokenUsageAsync(
            accountId,
            sessionId,
            "RagChat",
            modelId,
            promptTokens,
            completionTokens,
            cancellationToken);

        // 10. Save User Question & AI Answer Messages
        var userMsg = new CourseChatMessage(sessionId, "user", sanitizedQuestion, null, 0, 0);
        var citationsJson = JsonSerializer.Serialize(citations);
        var aiMsg = new CourseChatMessage(sessionId, "assistant", answer, citationsJson, promptTokens, completionTokens);

        await _repository.AddMessageAsync(userMsg, cancellationToken);
        await _repository.AddMessageAsync(aiMsg, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AskRagQuestionResponseDto
        {
            MessageId = aiMsg.Id,
            Question = sanitizedQuestion,
            Answer = answer,
            Citations = citations,
            Status = "Success",
            IsViolation = false,
            WarningMessage = quotaCheck.WarningMessage
        };
    }
}
