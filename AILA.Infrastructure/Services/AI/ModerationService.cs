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
    // Regex cho các từ chửi bậy / xúc phạm / thô tục tiếng Việt & tiếng Anh (có ranh giới từ \b để tránh false positive)
    private static readonly Regex[] ToxicProfanityPatterns = new[]
    {
        // Viết tắt / Từ lóng chửi tục tiếng Việt
        new Regex(@"\b(đm|dm|đ\.m|d\.m|đkm|dkm|đcm|dcm|clmm|dmm|đmm|vcl|vkl|vl|v\.l|đéo|deo|địt|dit|đụ|cặc|kac|lồn|buồi|cứt|đái|ỉa|đĩ|cave|phò|cút)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        
        // Cụm từ chửi bới, xúc phạm, báng bổ
        new Regex(@"\b(địt\s*mẹ|dit\s*me|đụ\s*má|du\s*ma|đụ\s*mẹ|du\s*me|đụ\s*cha|đm\s*mày|dm\s*may|mẹ\s*mày|me\s*may|cha\s*mày|cha\s*may|bố\s*mày|bo\s*may|bà\s*mày|mả\s*mẹ)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"\b(con\s*đĩ|con\s*di|thằng\s*chó|thang\s*cho|con\s*chó|con\s*cho|chó\s*chết|cho\s*chet|đồ\s*chó|do\s*cho|chó\s*đẻ|cho\s*de)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"\b(thằng\s*ngu|thang\s*ngu|con\s*ngu|đồ\s*ngu|do\s*ngu|ngu\s*vcl|ngu\s*vkl|ngu\s*vl|ngu\s*như\s*bò|ngu\s*nhu\s*bo|ngu\s*như\s*chó|ngu\s*nhu\s*cho|óc\s*chó|oc\s*cho|óc\s*lợn|óc\s*bã\s*đậu)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"\b(mất\s*dạy|mat\s*day|khốn\s*nạn|khon\s*nan|khốn\s*khiếp|vô\s*học|vo\s*hoc|đồ\s*rác\s*rưởi|rác\s*rưởi|chết\s*tiệt|mẹ\s*kiếp)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"\b(vãi\s*lồn|vai\s*lon|vãi\s*cặc|vai\s*cac|vãi\s*đái|vai\s*dai|vãi\s*cứt|vai\s*cut|hãm\s*lồn|ham\s*lon|đậu\s*má|đờ\s*mờ)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"\b(ăn\s*cặc|an\s*cac|ăn\s*lồn|an\s*lon|bú\s*cu|bu\s*cu|bú\s*lồn|bu\s*lon|súc\s*vật|suc\s*vat)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),

        // Tiếng Anh
        new Regex(@"\b(fuck|fucking|fucker|motherfucker|shit|bullshit|bitch|bitches|asshole|bastard|cunt|dick|pussy|stfu|wtf|retard|idiot)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)
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

    // Danh sách mẫu nguy hại / bạo lực / thù hận / bất hợp pháp
    private static readonly Regex[] HarmfulPatterns = new[]
    {
        new Regex(@"\b(chế\s*tạo\s*bom|làm\s*vũ\s*khí|hack\s*tài\s*khoản|tấn\s*công\s*ddos|phá\s*hoại\s*hệ\s*thống|ma\s*túy|buôn\s*ma\s*túy|tự\s*tử|chém\s*giết|giết\s*người)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"\b(make\s*bomb|create\s*weapon|hack\s*account|ddos\s*attack|suicide|kill\s*someone|illegal\s*drugs)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)
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

        // 2. Kiểm tra từ thô tục / chửi bới / xúc phạm (Toxic & Profanity & Insults)
        foreach (var pattern in ToxicProfanityPatterns)
        {
            if (pattern.IsMatch(normalizedInput))
            {
                return Task.FromResult((false, "Nội dung chứa từ ngữ thô tục, chửi bới hoặc xúc phạm không phù hợp với quy chuẩn đào tạo."));
            }
        }

        // 3. Kiểm tra nội dung nguy hại / độc hại cao (Harmful & Violent Content)
        foreach (var pattern in HarmfulPatterns)
        {
            if (pattern.IsMatch(normalizedInput))
            {
                return Task.FromResult((false, "Nội dung vi phạm chính sách an toàn (chứa thông tin bạo lực, vi phạm pháp luật hoặc an ninh mạng)."));
            }
        }

        return Task.FromResult((true, string.Empty));
    }
}
