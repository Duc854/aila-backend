// AILA.Infrastructure.Services.AI/RoleParserService.cs
using AILA.Application.Common.Dtos;
using AILA.Application.Common.Interfaces.AI;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace AILA.Infrastructure.Services.AI;

public class RoleParserService : IRoleParserService
{
    private readonly ILogger<RoleParserService> _logger;

    public RoleParserService(ILogger<RoleParserService> logger)
    {
        _logger = logger;
    }

    public Task<RoleParseResultDto> ParseRolesAsync(string aiTask, CancellationToken cancellationToken = default)
    {
        var result = TryParseWithRegex(aiTask);

        if (result.IsSuccess)
        {
            _logger.LogInformation("✅ Parse roles by Regex success: UserRole={UserRole}, AIRole={AIRole}",
                result.UserRole, result.AIRole);
        }
        else
        {
            _logger.LogWarning("⚠️ Phân tích vai trò bằng Regex thất bại — sử dụng vai trò mặc định.");
        }

        return Task.FromResult(result);
    }

    private static RoleParseResultDto TryParseWithRegex(string aiTask)
    {
        if (string.IsNullOrWhiteSpace(aiTask))
        {
            return new RoleParseResultDto
            {
                UserRole = "Chưa xác định",
                AIRole = "Chưa xác định",
                IsSuccess = false,
                ErrorMessage = "Nhiệm vụ AI (AITask) bị trống."
            };
        }

        var userRole = "Chưa xác định";
        var aiRole   = "Chưa xác định";

        // 1. AI Role patterns
        if (aiRole == "Chưa xác định")
        {
            // Pattern: "Bạn LÀ X (Y)"
            var m = Regex.Match(aiTask, @"\bBạn LÀ\s+([^(]+)\(([^)]+)\)", RegexOptions.IgnoreCase);
            if (m.Success) aiRole = CleanRole(m.Groups[2].Value);
        }

        if (aiRole == "Chưa xác định")
        {
            // Pattern: "AI role: X" or "Vai trò AI: X" or "Vai trò của AI: X"
            var m = Regex.Match(aiTask, @"\b(?:AI\s*role|Vai\s*trò\s*(?:của\s*)?AI|Vai\s*trò\s*của\s*bạn):\s*([^,.\n]+)", RegexOptions.IgnoreCase);
            if (m.Success) aiRole = CleanRole(m.Groups[1].Value);
        }

        if (aiRole == "Chưa xác định")
        {
            // Pattern: "AI đóng vai X" or "Bạn đóng vai X" or "Đóng vai là X" (khi đứng đầu câu) or "Bạn trong vai trò X" or "Giả sử bạn là X"
            var m = Regex.Match(aiTask, @"(?:(?:^|[.\n])\s*đóng\s*vai\s*(?:là)?|\b(?:AI|Bạn)\s+đóng\s*vai\s*(?:là)?|\bBạn\s+trong\s*vai\s*trò\s*(?:là)?|\bGiả\s*sử\s*bạn\s*là)\s+([^,.\n]+)", RegexOptions.IgnoreCase);
            if (m.Success) aiRole = CleanRole(m.Groups[1].Value);
        }

        if (aiRole == "Chưa xác định")
        {
            // Pattern: "Bạn là X"
            var m = Regex.Match(aiTask, @"\bBạn\s+là\s+([^,.\n]+)", RegexOptions.IgnoreCase);
            if (m.Success) aiRole = CleanRole(m.Groups[1].Value);
        }

        if (aiRole == "Chưa xác định")
        {
            // Pattern English: "You are (an?|the) X" or "Act as (an?|the) X"
            var m = Regex.Match(aiTask, @"\b(?:You\s+are|Act\s+as)\s+(?:an?|the)?\s*([^,.\n]+)", RegexOptions.IgnoreCase);
            if (m.Success) aiRole = CleanRole(m.Groups[1].Value);
        }

        // 2. User Role patterns
        if (userRole == "Chưa xác định")
        {
            // Pattern: "User role: X" or "Vai trò User: X" or "Vai trò người dùng/học viên: X"
            var m = Regex.Match(aiTask, @"\b(?:User\s*role|Learner\s*role|Student\s*role|Vai\s*trò\s*(?:của\s*)?(?:User|người\s*dùng|học\s*viên|người\s*học)):\s*([^,.\n]+)", RegexOptions.IgnoreCase);
            if (m.Success) userRole = CleanRole(m.Groups[1].Value);
        }

        if (userRole == "Chưa xác định")
        {
            // Pattern: "Người đang chat với bạn LÀ X" or "Người dùng/Học viên đóng vai X"
            var m = Regex.Match(aiTask, @"\b(?:Người\s*đang\s*chat\s*với\s*bạn\s*LÀ|Học\s*viên\s*(?:đóng\s*vai|là)|Người\s*dùng\s*(?:đóng\s*vai|là)|Người\s*học\s*(?:đóng\s*vai|là))\s+([^,.\n]+)", RegexOptions.IgnoreCase);
            if (m.Success) userRole = CleanRole(m.Groups[1].Value);
        }

        if (userRole == "Chưa xác định")
        {
            // Pattern English: "User acts as X" or "The student is X"
            var m = Regex.Match(aiTask, @"\b(?:The\s+user\s+is|User\s+acts\s+as|Student\s+is)\s+(?:an?|the)?\s*([^,.\n]+)", RegexOptions.IgnoreCase);
            if (m.Success) userRole = CleanRole(m.Groups[1].Value);
        }

        if (userRole == "Chưa xác định" && aiRole == "Chưa xác định")
        {
            return new RoleParseResultDto
            {
                UserRole = "Chưa xác định",
                AIRole = "Chưa xác định",
                IsSuccess = false,
                ErrorMessage = "Không thể phân tích vai trò từ nhiệm vụ AI."
            };
        }

        return new RoleParseResultDto
        {
            UserRole = userRole,
            AIRole   = aiRole,
            IsSuccess = userRole != "Chưa xác định" && aiRole != "Chưa xác định",
            ErrorMessage = (userRole == "Chưa xác định" || aiRole == "Chưa xác định")
                ? "Chỉ phân tích được một phần: có vai trò chưa xác định."
                : null
        };
    }

    private static string CleanRole(string role)
    {
        if (string.IsNullOrWhiteSpace(role)) return "Chưa xác định";
        var cleaned = role.Trim().Trim('\'', '"', '.', ',', ';', ':', '(', ')', '[', ']');
        return string.IsNullOrWhiteSpace(cleaned) ? "Chưa xác định" : cleaned;
    }
}
