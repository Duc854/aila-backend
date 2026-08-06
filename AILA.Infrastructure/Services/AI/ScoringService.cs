using AILA.Application.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AILA.Application.Common.Dtos;
using AILA.Application.Common.Dtos.AI;
using AILA.Application.Common.Interfaces.AI;
using AILA.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace AILA.Infrastructure.Services.AI;

public class ScoringService : IScoringService
{
    private readonly IChatCompletionService _chatCompletion;
    private readonly IRoleParserService _roleParser;
    private readonly IQuotaService _quotaService;
    private readonly string _modelId;

    private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
    };

    public ScoringService(
        IChatCompletionService chatCompletion,
        IConfiguration configuration,
        IRoleParserService roleParser,
        IQuotaService quotaService)
    {
        _chatCompletion = chatCompletion;
        _roleParser = roleParser;
        _quotaService = quotaService;
        _modelId = configuration["OpenAI:ModelId"] ?? "llama-3.1-8b-instant";
    }

    public async Task<ScoringEvaluationResult> EvaluateSubmissionAsync(
        Guid submissionId,
        string userPrompt,
        string aiResponse,
        List<ScoringCriteria> criteria,
        int retryLimit = 2,
        CancellationToken cancellationToken = default)
    {
        if (criteria == null || !criteria.Any())
        {
            return new ScoringEvaluationResult(new List<CriteriaScore>(), string.Empty);
        }

        var criteriaJson = JsonSerializer.Serialize(criteria.Select(c => new {
            c.Id,
            c.Title,
            c.Description,
            MaxScore = (int)c.Weight
        }));

        var sampleGuid = criteria.FirstOrDefault()?.Id.ToString() ?? Guid.NewGuid().ToString();

        var systemInstruction = @"Bạn là Chuyên gia Đánh giá và Chấm điểm bài tập Prompt Engineering.
Nhiệm vụ: Phân tích cuộc hội thoại và chấm điểm từng tiêu chí được cung cấp.

QUY TẮC BẮT BUỘC:
1. Đánh giá KHÁCH QUAN, CHÍNH XÁC dựa trên nội dung thực tế của User Prompt và AI Response.
2. Với mỗi tiêu chí, tính số điểm đạt được (tối đa bằng MaxScore của tiêu chí đó).
3. Đưa ra lời nhận xét (Feedback) cụ thể, ngắn gọn bằng tiếng Việt giúp học viên cải thiện.
4. Trả về ĐÚNG định dạng JSON hợp lệ theo schema yêu cầu. KHÔNG thêm bất kỳ văn bản nào ngoài JSON.";

        var promptBuilder = new System.Text.StringBuilder();
        promptBuilder.AppendLine("--- DANH SÁCH TIÊU CHÍ CHẤM ĐIỂM ---");
        promptBuilder.AppendLine(criteriaJson);
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("--- DỮ LIỆU CẦN ĐÁNH GIÁ ---");
        promptBuilder.AppendLine($"User Prompt: \"{userPrompt}\"");
        promptBuilder.AppendLine($"AI Response: \"{aiResponse}\"");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("--- YÊU CẦU ĐẦU RA (JSON SCHEMA) ---");
        promptBuilder.AppendLine("{");
        promptBuilder.AppendLine("  \"scores\": [");
        promptBuilder.AppendLine("    {");
        promptBuilder.AppendLine($"      \"criteriaId\": \"{sampleGuid}\",");
        promptBuilder.AppendLine("      \"score\": 25,");
        promptBuilder.AppendLine("      \"feedback\": \"Học viên đã thiết lập bối cảnh rõ ràng nhưng chưa nêu rõ định dạng mong muốn.\"");
        promptBuilder.AppendLine("    }");
        promptBuilder.AppendLine("  ]");
        promptBuilder.AppendLine("}");

        var userMessage = promptBuilder.ToString();

        for (int attempt = 0; attempt <= retryLimit; attempt++)
        {
            try
            {
                var rawResponse = await CallChatApiWithSystemAsync(systemInstruction, userMessage, 0.2f, attemptId: null, accountId: null, cancellationToken: cancellationToken);
                var cleanedJson = CleanJsonContent(rawResponse);

                using var doc = JsonDocument.Parse(cleanedJson);
                var root = doc.RootElement;

                if (root.TryGetProperty("scores", out var scoresElem) && scoresElem.ValueKind == JsonValueKind.Array)
                {
                    var resultScores = new List<CriteriaScore>();

                    foreach (var elem in scoresElem.EnumerateArray())
                    {
                        var critIdStr = elem.GetProperty("criteriaId").GetString();
                        var scoreVal = elem.GetProperty("score").GetDecimal();
                        var feedbackStr = elem.GetProperty("feedback").GetString() ?? string.Empty;

                        if (Guid.TryParse(critIdStr, out var critId))
                        {
                            var matchingCriteria = criteria.FirstOrDefault(c => c.Id == critId);
                            if (matchingCriteria != null)
                            {
                                if (scoreVal > matchingCriteria.Weight) scoreVal = matchingCriteria.Weight;
                                if (scoreVal < 0) scoreVal = 0;

                                resultScores.Add(new CriteriaScore(submissionId, critId, scoreVal, feedbackStr));
                            }
                        }
                    }

                    foreach (var c in criteria)
                    {
                        if (!resultScores.Any(rs => rs.CriteriaId == c.Id))
                        {
                            resultScores.Add(new CriteriaScore(submissionId, c.Id, 0, "Chưa được đánh giá do lỗi phản hồi AI."));
                        }
                    }

                    return new ScoringEvaluationResult(resultScores, string.Empty);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ScoringService via Semantic Kernel] Attempt {attempt + 1} failed parsing evaluation: {ex.Message}");
            }
        }

        var fallbackScores = criteria.Select(c => new CriteriaScore(submissionId, c.Id, 0, "Không thể chấm điểm do hệ thống AI bận.")).ToList();
        return new ScoringEvaluationResult(fallbackScores, string.Empty);
    }

    public async Task<OverallScoringResult> GenerateOverallSuggestionAsync(
        List<PromptSubmission> submissions,
        string scenario,
        string learnerTask,
        List<ScoringCriteria> criteria,
        string aiTask,
        Guid? attemptId = null,
        Guid? accountId = null,
        CancellationToken cancellationToken = default)
    {
        if (submissions == null || !submissions.Any())
        {
            return CreateDefaultEmptyResult(submissions?.Count ?? 0, scenario, criteria);
        }

        try
        {
            var roleResult = await _roleParser.ParseRolesAsync(aiTask, cancellationToken);
            var roleContext = roleResult.IsSuccess
                ? $"\n- Bối cảnh vai diễn: Học viên đóng vai '{roleResult.UserRole}', AI đóng vai '{roleResult.AIRole}'."
                : string.Empty;

            var conversationBuilder = new System.Text.StringBuilder();
            int turn = 1;
            foreach (var sub in submissions.OrderBy(s => s.CreatedAt))
            {
                if (sub.IsRejected)
                {
                    conversationBuilder.AppendLine($"Lượt {turn} (VI PHẠM): Học viên gửi: \"{sub.UserPrompt}\" -> Hệ thống chặn: {sub.RejectionReason}");
                }
                else
                {
                    conversationBuilder.AppendLine($"Lượt {turn}:");
                    conversationBuilder.AppendLine($"  - Học viên (User): \"{sub.UserPrompt}\"");
                    conversationBuilder.AppendLine($"  - Đối phương (AI): \"{sub.AiResponse}\"");
                }
                turn++;
            }

            var criteriaJson = JsonSerializer.Serialize(criteria.Select(c => new {
                c.Id,
                c.Title,
                c.Description,
                MaxScore = (int)c.Weight
            }));

            var systemInstruction = @"Bạn là Chuyên gia Đánh giá Năng lực Prompt Engineering hàng đầu.
Nhiệm vụ của bạn là phân tích TOÀN BỘ CUỘC HỘI THOẠI giữa Học viên và AI giả lập, từ đó chấm điểm tổng hợp, nhận xét chi tiết từng tiêu chí và đưa ra lời khuyên cải thiện.

QUY TẮC CHẤM ĐIỂM:
1. Đánh giá tính hiệu quả của các câu prompt do Học viên đặt dựa trên Kịch bản (Scenario) và Nhiệm vụ (LearnerTask).
2. Chấm điểm từng tiêu chí theo MaxScore tương ứng.
3. Tổng điểm (totalScore) = Tổng điểm các tiêu chí (tối đa 100).
4. Grade: 'Excellent' (>=85), 'Pass' (>=60), 'NeedsImprovement' (<60).
5. Trả về DUY NHẤT một chuỗi JSON hợp lệ. TUYỆT ĐỐI KHÔNG thêm lời mở đầu hay kết luận ngoài JSON.";

            var userMessage = $@"--- BỐI CẢNH BÀI TẬP ---
- Kịch bản: {scenario}
- Nhiệm vụ học viên: {learnerTask}{roleContext}

--- DANH SÁCH TIÊU CHÍ CHẤM ĐIỂM ---
{criteriaJson}

--- NHẬT KÝ HỘI THOẠI CỦA HỌC VIÊN ---
{conversationBuilder}

--- YÊU CẦU ĐẦU RA (ĐÚNG ĐỊNH DẠNG JSON SCHEMA NÀY) ---
{{
  ""totalScore"": 85,
  ""maxScore"": 100,
  ""percentage"": 85,
  ""grade"": ""Excellent"",
  ""summary"": ""Học viên đã thể hiện tốt kỹ năng đặt prompt rõ ràng..."",
  ""criteria"": [
    {{
      ""criteriaId"": ""{criteria.FirstOrDefault()?.Id}"",
      ""criteriaName"": ""{criteria.FirstOrDefault()?.Title}"",
      ""score"": 25,
      ""maxScore"": 30,
      ""status"": ""Đạt"",
      ""evaluation"": ""Học viên đã mô tả rõ vai trò..."",
      ""suggestion"": ""Cần thêm chi tiết về định dạng đầu ra.""
    }}
  ],
  ""detectedIssues"": [
    ""Chưa chỉ định rõ tone giọng mong muốn ở lượt prompt 1.""
  ],
  ""learningSuggestions"": [
    ""Nên áp dụng kỹ thuật Few-Shot Prompting để kết quả AI trả về chính xác hơn.""
  ],
  ""nextPromptExample"": ""Ví dụ câu thoại mẫu: 'Hãy đóng vai Mentor tư vấn 5 ý tưởng đồ án...""
}}";

            var rawResponse = await CallChatApiWithSystemAsync(systemInstruction, userMessage, 0.3f, attemptId, accountId, cancellationToken);
            var cleanedJson = CleanJsonContent(rawResponse);
            Console.WriteLine($"🔥 [ScoringService Cleaned JSON via Semantic Kernel]:\n{cleanedJson}");

            var result = JsonSerializer.Deserialize<OverallScoringResult>(cleanedJson, _jsonOptions);

            if (result != null)
            {
                if (roleResult.IsSuccess && !string.IsNullOrEmpty(roleResult.UserRole) && !string.IsNullOrEmpty(result.Summary))
                {
                    var cleanUserRole = roleResult.UserRole.Trim();
                    if (!result.Summary.Contains(cleanUserRole, StringComparison.OrdinalIgnoreCase))
                    {
                        result.Summary = System.Text.RegularExpressions.Regex.Replace(
                            result.Summary,
                            @"vai trò '([^']+)'",
                            $"vai trò '{cleanUserRole}'",
                            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    }
                }

                if (result.Criteria == null) result.Criteria = new List<CriteriaScoreDto>();

                for (int i = 0; i < result.Criteria.Count; i++)
                {
                    var cDto = result.Criteria[i];
                    var matchCriteria = criteria.FirstOrDefault(c => 
                        c.Id.ToString().Equals(cDto.CriteriaId, StringComparison.OrdinalIgnoreCase) ||
                        c.Title.Equals(cDto.CriteriaName, StringComparison.OrdinalIgnoreCase))
                        ?? (i < criteria.Count ? criteria[i] : null);

                    if (matchCriteria != null)
                    {
                        cDto.CriteriaId = matchCriteria.Id.ToString();
                        cDto.CriteriaName = matchCriteria.Title;
                        cDto.MaxScore = (int)matchCriteria.Weight;
                        cDto.Feedback = string.IsNullOrWhiteSpace(cDto.Feedback) ? cDto.Evaluation : cDto.Feedback;
                        cDto.CreatedAt = DateTime.UtcNow;
                        cDto.Id = Guid.NewGuid();
                    }
                }

                result.Metadata = new MetadataDto
                {
                    ConversationAnalyzed = submissions.Count,
                    ValidPrompts = submissions.Count(s => !s.IsRejected),
                    InvalidPrompts = submissions.Count(s => s.IsRejected),
                    ScenarioUsed = !string.IsNullOrEmpty(scenario),
                    UserTaskCompleted = true
                };
                return result;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ScoringService via Semantic Kernel] Error generating overall suggestion: {ex.Message}\nStack: {ex.StackTrace}");
        }

        return CreateDefaultEmptyResult(submissions?.Count ?? 0, scenario, criteria);
    }

    public async Task<string> GeneratePromptSuggestionAsync(
        string userPrompt,
        string aiResponse,
        List<ScoringCriteria> criteriaList,
        CancellationToken cancellationToken = default)
    {
        var prompt = $"User prompt: {userPrompt}\nAI response: {aiResponse}\nHãy gợi ý 1 câu prompt mẫu đạt điểm tối đa.";
        try
        {
            return await CallChatApiAsync(prompt, 0.5f, cancellationToken);
        }
        catch
        {
            return string.Empty;
        }
    }

    private OverallScoringResult CreateDefaultEmptyResult(int submissionCount, string scenario, List<ScoringCriteria> criteria)
    {
        return new OverallScoringResult
        {
            Summary = "Hệ thống AI chấm điểm tạm thời gián đoạn (Rate Limit hoặc kết nối API bận). Vui lòng thử lại sau vài giây.",
            Grade = "SystemBusy",
            Percentage = 0,
            TotalScore = 0,
            MaxScore = 100,
            Criteria = criteria.Select(c => new CriteriaScoreDto
            {
                Id = Guid.NewGuid(),
                CriteriaId = c.Id.ToString(),
                CriteriaName = c.Title,
                Score = 0,
                MaxScore = (int)c.Weight,
                Status = "Chưa chấm được",
                Evaluation = "Hệ thống AI bận nên chưa thể phân tích tiêu chí này.",
                Suggestion = "Vui lòng bấm gửi lại hoặc chấm lại.",
                Feedback = "Hệ thống AI bận.",
                CreatedAt = DateTime.UtcNow
            }).ToList(),
            DetectedIssues = new List<string> { "API LLM bị gián đoạn hoặc quá tải (HTTP 429 / Timeout)." },
            LearningSuggestions = new List<string> { "Hệ thống đã tự động retry. Bạn có thể thử kết thúc bài lại." },
            NextPromptExample = "",
            Metadata = new MetadataDto
            {
                ConversationAnalyzed = submissionCount,
                ValidPrompts = submissionCount,
                InvalidPrompts = 0,
                ScenarioUsed = !string.IsNullOrEmpty(scenario),
                UserTaskCompleted = false
            }
        };
    }

    private string CleanJsonContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return string.Empty;

        var firstBrace = content.IndexOf('{');
        var lastBrace = content.LastIndexOf('}');

        if (firstBrace >= 0 && lastBrace > firstBrace)
        {
            return content.Substring(firstBrace, lastBrace - firstBrace + 1).Trim();
        }

        return content.Trim();
    }

    private async Task<string> CallChatApiAsync(string userMessage, float temperature, CancellationToken cancellationToken)
    {
        return await CallChatApiWithSystemAsync("You are a helpful assistant.", userMessage, temperature, attemptId: null, accountId: null, cancellationToken: cancellationToken);
    }

    private async Task<string> CallChatApiWithSystemAsync(
        string systemInstruction,
        string userMessage,
        float temperature,
        Guid? attemptId = null,
        Guid? accountId = null,
        CancellationToken cancellationToken = default)
    {
        int maxRetries = 5;
        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                var chatHistory = new ChatHistory();
                chatHistory.AddSystemMessage(systemInstruction);
                chatHistory.AddUserMessage(userMessage);

                var executionSettings = new OpenAIPromptExecutionSettings
                {
                    Temperature = temperature
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

                Console.WriteLine($"🔥 [TOKEN USED - SCORING VIA SEMANTIC KERNEL]: PromptTokens={promptTokens}, CompletionTokens={completionTokens}, TotalTokens={promptTokens + completionTokens}");

                var targetAccountId = accountId ?? Guid.Empty;

                if (targetAccountId != Guid.Empty)
                {
                    await _quotaService.RecordTokenUsageAsync(
                        targetAccountId,
                        attemptId,
                        "Scoring",
                        _modelId,
                        promptTokens,
                        completionTokens,
                        cancellationToken);
                }

                return response.Content ?? string.Empty;
            }
            catch (Exception ex) when (attempt < maxRetries - 1 &&
                (ex.Message.Contains("429") || ex.Message.Contains("rate", StringComparison.OrdinalIgnoreCase) ||
                 ex.Message.Contains("Too Many", StringComparison.OrdinalIgnoreCase) ||
                 ex.Message.Contains("overloaded", StringComparison.OrdinalIgnoreCase)))
            {
                // Exponential backoff: 5s, 10s, 15s, 20s (tổng ~50s, đủ để Groq reset rate limit 1 phút)
                var delaySeconds = (attempt + 1) * 5;
                Console.WriteLine($"⚠️ [ScoringService Rate Limit] Retrying in {delaySeconds}s (Attempt {attempt + 1}/{maxRetries}): {ex.Message}");
                await Task.Delay(delaySeconds * 1000, cancellationToken);
            }
        }

        return string.Empty;
    }
}
