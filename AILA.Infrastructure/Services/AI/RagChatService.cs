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

        // 4. Fetch Chat Session & Validate Ownership & Enrollment
        var session = await _repository.GetSessionByIdAsync(sessionId, cancellationToken);
        if (session == null)
        {
            throw new InvalidOperationException($"Không tìm thấy phiên trò chuyện RAG ID: {sessionId}");
        }

        // Chống IDOR: Chỉ chủ sở hữu session mới được hỏi đáp trong session này
        if (session.AccountId != accountId)
        {
            return new AskRagQuestionResponseDto
            {
                MessageId = Guid.Empty,
                Question = sanitizedQuestion,
                Answer = "Bạn không có quyền truy cập vào phiên trò chuyện này.",
                Status = "Forbidden",
                IsViolation = false,
                WarningMessage = "Quyền truy cập không hợp lệ."
            };
        }

        // Kiểm tra quyền Enrollment của học viên trong khóa học
        var isEnrolled = await _repository.IsLearnerEnrolledInCourseAsync(accountId, session.CourseId, cancellationToken);
        if (!isEnrolled)
        {
            return new AskRagQuestionResponseDto
            {
                MessageId = Guid.Empty,
                Question = sanitizedQuestion,
                Answer = "Bạn cần đăng ký khóa học này trước khi sử dụng Trợ lý AI.",
                Status = "Forbidden",
                IsViolation = false,
                WarningMessage = "Tài khoản chưa đăng ký khóa học này."
            };
        }

        // Tự động cập nhật tiêu đề session từ câu hỏi đầu tiên
        if (session.Title == "Cuộc trò chuyện mới" || string.IsNullOrWhiteSpace(session.Title))
        {
            var newTitle = sanitizedQuestion.Length > 50 ? sanitizedQuestion.Substring(0, 47) + "..." : sanitizedQuestion;
            session.UpdateTitle(newTitle);
        }

        // 5. Generate Vector Embedding for user question & Retrieve top chunks with similarity >= 0.60
        var queryEmbedding = await _knowledgeBaseService.GenerateEmbeddingAsync(sanitizedQuestion, cancellationToken);
        var similarChunks = await _repository.SearchSimilarChunksAsync(session.CourseId, queryEmbedding, topK: 3, minSimilarity: 0.60, cancellationToken);

        // 6. Build Citations List & Context (ONLY if chunks genuinely match the question)
        var citations = new List<RagCitationDto>();
        var contextTextBuilder = new System.Text.StringBuilder();

        bool hasRelevantCourseContent = similarChunks.Any();

        if (hasRelevantCourseContent)
        {
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
                    SimilarityScore = realScore
                });

                contextTextBuilder.AppendLine($"--- [Trích dẫn từ bài học: {title}] ---");
                contextTextBuilder.AppendLine(chunk.Content);
                contextTextBuilder.AppendLine();
            }
        }

        // 7. Build RAG Prompt dynamically based on relevance
        string systemInstruction;
        if (hasRelevantCourseContent)
        {
            systemInstruction = @"Bạn là trợ lý AI thông minh phụ trách giải đáp thắc mắc cho Học viên trong khóa học.
Dưới đây là NỘI DUNG TÀI LIỆU BÀI HỌC liên quan trực tiếp đến câu hỏi được trích xuất từ hệ thống:
" + contextTextBuilder.ToString() + @"
YÊU CẦU TRẢ LỜI:
1. Hãy sử dụng NỘI DUNG TÀI LIỆU BÀI HỌC ở trên để giải đáp chính xác, rõ ràng và mạch lạc cho Học viên.
2. Trả lời bằng tiếng Việt, thái độ hỗ trợ nhiệt tình, dễ hiểu.";
        }
        else
        {
            systemInstruction = @"Bạn là trợ lý AI thông minh phụ trách hỗ trợ và giải đáp thắc mắc cho Học viên trong khóa học.
HƯỚNG DẪN TRẢ LỜI:
1. NẾU NGƯỜI DÙNG CHÀO HỎI HOẶC GIAO TIẾP XÃ GIAO (ví dụ: 'hello', 'hi', 'chào bạn', 'cảm ơn'): Hãy chào lại một cách thân thiện, tự nhiên và sẵn sàng giải đáp các câu hỏi về khóa học. TUYỆT ĐỐI KHÔNG tự ý đưa ra các bài học cụ thể hay giới thiệu tài liệu khi người dùng chưa hỏi.
2. NẾU NGƯỜI DÙNG HỎI KIẾN THỨC CHUNG HOẶC NGOÀI KHÓA HỌC: Hãy vận dụng kiến thức chuyên môn rộng lớn của bạn để giải đáp chi tiết, chu đáo và hữu ích cho Học viên (TUYỆT ĐỐI KHÔNG từ chối trả lời hoặc bảo 'tôi không biết').
3. Trả lời bằng tiếng Việt, lịch sự, thân thiện và mạch lạc.";
        }

        // 8. Fetch Recent Conversation History for Multi-turn Context via SQL pagination
        var recentHistory = await _repository.GetRecentMessagesAsync(sessionId, 6, cancellationToken);
        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(systemInstruction);

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
        var modelId = _configuration["OpenAI:ModelId"] ?? "llama-3.1-8b-instant";
        var (promptTokens, completionTokens) = TokenUsageExtractor.Extract(response, promptText, answer, modelId);

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
