using AILA.Application.Common.Interfaces.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace AILA.Infrastructure.Services.AI;

public class ModerationService : IModerationService
{
    // Danh sách từ cấm / thô tục (Profanity & Toxic Speech)
    private static readonly string[] ToxicKeywords = new[]
    {
        "đm", "đ.m", "dm", "dkm", "đkm", "vkl", "vcl", "v l", "đéo", "đeo",
        "dcm", "đcm", "vl", "clmm", "đái", "ỉa", "fuck", "shit", "bitch",
        "asshole", "bastard", "cứt", "chửi", "ngu vcl", "con mẹ", "đòn vọt"
    };

    // Danh sách mẫu Prompt Injection / Jailbreak Attack
    private static readonly Regex[] InjectionPatterns = new[]
    {
        new Regex(@"bỏ\s+qua\s+(tất\s+cả\s+)?(quy\s+tắc|hướng\s+dẫn|chính\s+sách|lệnh)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"ignore\s+(all\s+)?(previous\s+)?(instructions|rules|prompts|system)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"(in|hiển\s+thị|tiết\s+lộ|show|print|reveal)\s+(ra\s+)?(toàn\s+bộ\s+)?(system\s+prompt|aitask|mật\s+mã|cấu\s+hình)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"you\s+are\s+now\s+in\s+dan\s+mode", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"do\s+anything\s+now", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"override\s+(system|rules)", RegexOptions.IgnoreCase | RegexOptions.Compiled)
    };

    // Danh sách mẫu nguy hại / bạo lực / thù hận
    private static readonly string[] HarmfulKeywords = new[]
    {
        "chế tạo bom", "làm vũ khí", "hack tài khoản", "tấn công ddos",
        "phá hoại hệ thống", "ma túy", "tự tử", "chém giết"
    };

    public Task<(bool IsSafe, string Reason)> CheckContentSafetyAsync(string input, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return Task.FromResult((true, string.Empty));
        }

        var normalizedInput = input.Trim();

        // 1. Kiểm tra Prompt Injection / Jailbreak Attack
        foreach (var pattern in InjectionPatterns)
        {
            if (pattern.IsMatch(normalizedInput))
            {
                return Task.FromResult((false, "Phát hiện hành vi Prompt Injection (Cố tình phá vỡ quy tắc hoặc khai thác thông tin hệ thống)."));
            }
        }

        // 2. Kiểm tra từ thô tục / độc hại (Toxic & Profanity)
        var words = normalizedInput.ToLowerInvariant().Split(new[] { ' ', '.', ',', '!', '?', ';', ':', '-', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var toxic in ToxicKeywords)
        {
            if (words.Any(w => w.Equals(toxic, StringComparison.OrdinalIgnoreCase)) ||
                (toxic.Length >= 4 && normalizedInput.Contains(toxic, StringComparison.OrdinalIgnoreCase)))
            {
                return Task.FromResult((false, "Nội dung chứa từ ngữ thô tục hoặc không phù hợp với quy chuẩn đào tạo."));
            }
        }

        // 3. Kiểm tra nội dung nguy hại / độc hại cao (Harmful Content)
        foreach (var harmful in HarmfulKeywords)
        {
            if (normalizedInput.Contains(harmful, StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult((false, "Nội dung vi phạm chính sách an toàn (chứa thông tin bạo lực, vi phạm pháp luật hoặc an ninh mạng)."));
            }
        }

        return Task.FromResult((true, string.Empty));
    }
}
