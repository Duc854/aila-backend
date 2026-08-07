// AILA.Infrastructure.Services.AI/RoleParserService.cs
using AILA.Application.Common.Dtos;
using AILA.Application.Common.Interfaces.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AILA.Infrastructure.Services.AI;

public class RoleParserService : IRoleParserService
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly ILogger<RoleParserService> _logger;

    public RoleParserService(
        IConfiguration configuration,
        HttpClient httpClient,
        ILogger<RoleParserService> logger)
    {
        _configuration = configuration;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<RoleParseResultDto> ParseRolesAsync(string aiTask, CancellationToken cancellationToken = default)
    {
        // 1. Try parse bằng Regex trước (nhanh)
        var regexResult = TryParseWithRegex(aiTask);
        if (regexResult.IsSuccess)
        {
            _logger.LogInformation("✅ Parse roles by Regex success: UserRole={UserRole}, AIRole={AIRole}",
                regexResult.UserRole, regexResult.AIRole);
            return regexResult;
        }

        // 2. Nếu Regex không parse được → dùng AI
        _logger.LogWarning("⚠️ Regex parse failed, falling back to AI for role parsing");
        var aiResult = await ParseWithAIAsync(aiTask, cancellationToken);

        if (aiResult.IsSuccess)
        {
            _logger.LogInformation("✅ Parse roles by AI success: UserRole={UserRole}, AIRole={AIRole}",
                aiResult.UserRole, aiResult.AIRole);
            return aiResult;
        }

        // 3. Nếu AI cũng fail → trả về Unknown
        _logger.LogError("❌ Both Regex and AI failed to parse roles from AITask");
        return new RoleParseResultDto
        {
            UserRole = "Unknown",
            AIRole = "Unknown",
            IsSuccess = false,
            ErrorMessage = "Cannot parse roles from AITask. Please check AITask format."
        };
    }

    private RoleParseResultDto TryParseWithRegex(string aiTask)
    {
        if (string.IsNullOrWhiteSpace(aiTask))
        {
            return new RoleParseResultDto
            {
                UserRole = "Unknown",
                AIRole = "Unknown",
                IsSuccess = false,
                ErrorMessage = "AITask is null or empty"
            };
        }

        var userRole = "Unknown";
        var aiRole = "Unknown";
        var found = false;

        // Pattern 1: "Bạn LÀ X (Y)" → aiRole = Y
        var match1 = Regex.Match(aiTask, @"Bạn LÀ\s+([^(]+)\(([^)]+)\)");
        if (match1.Success)
        {
            aiRole = match1.Groups[2].Value.Trim();
            found = true;
        }

        // Pattern 2: "Người đang chat với bạn LÀ X" → userRole = X
        var match2 = Regex.Match(aiTask, @"Người đang chat với bạn LÀ\s+([^.]+)");
        if (match2.Success)
        {
            userRole = match2.Groups[1].Value.Trim();
            found = true;
        }

        // Pattern 3: "Bạn là X, Y" → aiRole = X
        if (!found)
        {
            var match3 = Regex.Match(aiTask, @"Bạn là\s+([^,]+)");
            if (match3.Success)
            {
                aiRole = match3.Groups[1].Value.Trim();
                found = true;
            }
        }

        // Pattern 4: "Bạn đóng vai X" → aiRole = X
        if (!found)
        {
            var match4 = Regex.Match(aiTask, @"đóng vai\s+([^.]+)");
            if (match4.Success)
            {
                aiRole = match4.Groups[1].Value.Trim();
                found = true;
            }
        }

        // Pattern 5: "Vai trò của bạn: X" → aiRole = X
        if (!found)
        {
            var match5 = Regex.Match(aiTask, @"Vai trò của bạn:\s*([^.]+)");
            if (match5.Success)
            {
                aiRole = match5.Groups[1].Value.Trim();
                found = true;
            }
        }

        // Pattern 6: "Vai trò của người dùng: X" → userRole = X
        var match6 = Regex.Match(aiTask, @"Vai trò của người dùng:\s*([^.]+)");
        if (match6.Success)
        {
            userRole = match6.Groups[1].Value.Trim();
            found = true;
        }

        // Pattern 7: "Người dùng đóng vai X" → userRole = X
        var match7 = Regex.Match(aiTask, @"Người dùng đóng vai\s+([^.]+)");
        if (match7.Success)
        {
            userRole = match7.Groups[1].Value.Trim();
            found = true;
        }

        // Pattern 8: "User role: X" → userRole = X
        var match8 = Regex.Match(aiTask, @"User role:\s*([^.]+)", RegexOptions.IgnoreCase);
        if (match8.Success)
        {
            userRole = match8.Groups[1].Value.Trim();
            found = true;
        }

        // Pattern 9: "AI role: X" → aiRole = X
        var match9 = Regex.Match(aiTask, @"AI role:\s*([^.]+)", RegexOptions.IgnoreCase);
        if (match9.Success)
        {
            aiRole = match9.Groups[1].Value.Trim();
            found = true;
        }

        // Pattern 10: "Vai trò AI: X" → aiRole = X
        if (!found)
        {
            var match10 = Regex.Match(aiTask, @"Vai trò AI:\s*([^.]+)", RegexOptions.IgnoreCase);
            if (match10.Success)
            {
                aiRole = match10.Groups[1].Value.Trim();
                found = true;
            }
        }

        // Pattern 11: "Vai trò User: X" → userRole = X
        var match11 = Regex.Match(aiTask, @"Vai trò User:\s*([^.]+)", RegexOptions.IgnoreCase);
        if (match11.Success)
        {
            userRole = match11.Groups[1].Value.Trim();
            found = true;
        }

        if (!found)
        {
            return new RoleParseResultDto
            {
                UserRole = "Unknown",
                AIRole = "Unknown",
                IsSuccess = false,
                ErrorMessage = "Cannot parse roles from AITask with Regex"
            };
        }

        return new RoleParseResultDto
        {
            UserRole = userRole,
            AIRole = aiRole,
            IsSuccess = userRole != "Unknown" && aiRole != "Unknown",
            ErrorMessage = (userRole == "Unknown" || aiRole == "Unknown") ? "Regex incomplete, falling back to AI" : null
        };
    }

    private async Task<RoleParseResultDto> ParseWithAIAsync(string aiTask, CancellationToken cancellationToken)
    {
        try
        {
            var systemPrompt = @"Bạn là chuyên gia phân tích vai trò trong kịch bản giao tiếp.

Nhiệm vụ: Đọc AITask và xác định:
1. AI đang đóng vai trò gì? (aiRole)
2. Người dùng (học viên) đang đóng vai trò gì? (userRole)

QUY TẮC:
- aiRole: Vai trò của AI trong cuộc hội thoại
- userRole: Vai trò của người dùng (học viên)
- Nếu không xác định được → trả về ""Unknown""
- Không được tự động gán default

Trả về DUY NHẤT JSON:
{
  ""aiRole"": ""<vai trò của AI hoặc Unknown>"",
  ""userRole"": ""<vai trò của người dùng hoặc Unknown>""
}

LƯU Ý: Chỉ trả về JSON, không có text khác.";

            var userMessage = $@"
Hãy phân tích AITask sau và xác định vai trò của AI và người dùng:

AITask:
{aiTask}";

            var apiKey = _configuration["OpenAI:ApiKey"];
            var modelId = _configuration["OpenAI:ModelId"] ?? "llama-3.1-8b-instant";
            var baseUrl = _configuration["OpenAI:BaseUrl"] ?? "https://api.openai.com/v1";

            if (string.IsNullOrEmpty(apiKey)) return new RoleParseResultDto { IsSuccess = false };

            var requestBody = new
            {
                model = modelId,
                messages = new object[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userMessage }
                },
                temperature = 0.2
            };

            var requestJson = JsonSerializer.Serialize(requestBody);
            var req = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl.TrimEnd('/')}/chat/completions")
            {
                Content = new StringContent(requestJson, System.Text.Encoding.UTF8, "application/json")
            };
            req.Headers.Add("Authorization", $"Bearer {apiKey}");

            var res = await _httpClient.SendAsync(req, cancellationToken);
            var resJson = await res.Content.ReadAsStringAsync(cancellationToken);
            if (!res.IsSuccessStatusCode) return new RoleParseResultDto { IsSuccess = false };

            using var doc = JsonDocument.Parse(resJson);
            var response = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";

            var cleaned = CleanJson(response);
            _logger.LogDebug("AI response for role parsing: {Response}", cleaned);

            var result = JsonSerializer.Deserialize<RoleParseResultDto>(cleaned, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result != null &&
                !string.IsNullOrEmpty(result.AIRole) &&
                !string.IsNullOrEmpty(result.UserRole))
            {
                result.IsSuccess = true;
                return result;
            }

            return new RoleParseResultDto
            {
                UserRole = "Unknown",
                AIRole = "Unknown",
                IsSuccess = false,
                ErrorMessage = "AI returned invalid result"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ AI role parsing failed");
            return new RoleParseResultDto
            {
                UserRole = "Unknown",
                AIRole = "Unknown",
                IsSuccess = false,
                ErrorMessage = $"AI parsing failed: {ex.Message}"
            };
        }
    }

    private string CleanJson(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return "{}";

        content = content.Trim();

        // Xóa markdown code blocks
        if (content.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
        {
            content = content.Substring(7);
        }
        else if (content.StartsWith("```"))
        {
            content = content.Substring(3);
        }

        if (content.EndsWith("```"))
        {
            content = content.Substring(0, content.Length - 3);
        }

        // Tìm vị trí bắt đầu JSON
        var startIndex = 0;
        for (int i = 0; i < content.Length; i++)
        {
            if (content[i] == '{' || content[i] == '[')
            {
                startIndex = i;
                break;
            }
        }
        if (startIndex > 0)
        {
            content = content.Substring(startIndex);
        }

        // Tìm vị trí kết thúc JSON
        var endIndex = content.Length;
        for (int i = content.Length - 1; i >= 0; i--)
        {
            if (content[i] == '}' || content[i] == ']')
            {
                endIndex = i + 1;
                break;
            }
        }
        if (endIndex < content.Length)
        {
            content = content.Substring(0, endIndex);
        }

        return content.Trim();
    }
}