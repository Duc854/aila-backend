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
        var found    = false;

        // Pattern 1: "Bạn LÀ X (Y)" → aiRole = Y
        var m1 = Regex.Match(aiTask, @"Bạn LÀ\s+([^(]+)\(([^)]+)\)");
        if (m1.Success) { aiRole = m1.Groups[2].Value.Trim(); found = true; }

        // Pattern 2: "Người đang chat với bạn LÀ X" → userRole = X
        var m2 = Regex.Match(aiTask, @"Người đang chat với bạn LÀ\s+([^.]+)");
        if (m2.Success) { userRole = m2.Groups[1].Value.Trim(); found = true; }

        // Pattern 3: "Bạn là X, Y" → aiRole = X
        if (!found)
        {
            var m3 = Regex.Match(aiTask, @"Bạn là\s+([^,]+)");
            if (m3.Success) { aiRole = m3.Groups[1].Value.Trim(); found = true; }
        }

        // Pattern 4: "Bạn đóng vai X" → aiRole = X
        if (!found)
        {
            var m4 = Regex.Match(aiTask, @"đóng vai\s+([^.]+)");
            if (m4.Success) { aiRole = m4.Groups[1].Value.Trim(); found = true; }
        }

        // Pattern 5: "Vai trò của bạn: X" → aiRole = X
        if (!found)
        {
            var m5 = Regex.Match(aiTask, @"Vai trò của bạn:\s*([^.]+)");
            if (m5.Success) { aiRole = m5.Groups[1].Value.Trim(); found = true; }
        }

        // Pattern 6: "Vai trò của người dùng: X" → userRole = X
        var m6 = Regex.Match(aiTask, @"Vai trò của người dùng:\s*([^.]+)");
        if (m6.Success) { userRole = m6.Groups[1].Value.Trim(); found = true; }

        // Pattern 7: "Người dùng đóng vai X" → userRole = X
        var m7 = Regex.Match(aiTask, @"Người dùng đóng vai\s+([^.]+)");
        if (m7.Success) { userRole = m7.Groups[1].Value.Trim(); found = true; }

        // Pattern 8: "User role: X" → userRole = X
        var m8 = Regex.Match(aiTask, @"User role:\s*([^.]+)", RegexOptions.IgnoreCase);
        if (m8.Success) { userRole = m8.Groups[1].Value.Trim(); found = true; }

        // Pattern 9: "AI role: X" → aiRole = X
        var m9 = Regex.Match(aiTask, @"AI role:\s*([^.]+)", RegexOptions.IgnoreCase);
        if (m9.Success) { aiRole = m9.Groups[1].Value.Trim(); found = true; }

        // Pattern 10: "Vai trò AI: X" → aiRole = X
        if (!found)
        {
            var m10 = Regex.Match(aiTask, @"Vai trò AI:\s*([^.]+)", RegexOptions.IgnoreCase);
            if (m10.Success) { aiRole = m10.Groups[1].Value.Trim(); found = true; }
        }

        // Pattern 11: "Vai trò User: X" → userRole = X
        var m11 = Regex.Match(aiTask, @"Vai trò User:\s*([^.]+)", RegexOptions.IgnoreCase);
        if (m11.Success) { userRole = m11.Groups[1].Value.Trim(); found = true; }

        if (!found)
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
}
