// AILA.Infrastructure.Services.AI/PromptValidationService.cs
using AILA.Application.Common.Interfaces.AI;
using AILA.Domain.Entities;

namespace AILA.Infrastructure.Services.AI;

public class PromptValidationService : IPromptValidationService
{
    private readonly IPrivacyService _privacyService;

    private const int MinLength = 5;
    private const int MinMeaningfulWords = 2;
    private const int MaxSpecialCharRatio = 50;
    private const int MaxSubmissionsPerMinute = 20; // Tối đa 20 prompt/phút (phù hợp test & sử dụng)
    private const int SimilarityThreshold = 80; // % giống nhau để coi là spam

    public PromptValidationService(
        IPrivacyService privacyService)
    {
        _privacyService = privacyService;
    }

    public Task<(bool IsValid, string? ViolationReason, string? PolicyName)> ValidateAsync(
        string prompt,
        PracticeAttempt attempt,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ValidateInternal(prompt, attempt));
    }

    private (bool IsValid, string? ViolationReason, string? PolicyName) ValidateInternal(
        string prompt,
        PracticeAttempt attempt)
    {
        // ============================================================
        // LEVEL 1: REJECT - Validate cơ bản (không cần DB)
        // ============================================================

        // 1. Prompt rỗng
        if (string.IsNullOrWhiteSpace(prompt))
        {
            return (false, "Nội dung prompt không được để trống.", "EmptyPrompt");
        }

        // 2. Prompt quá ngắn
        if (prompt.Length < MinLength)
        {
            return (false, $"Prompt quá ngắn (cần ít nhất {MinLength} ký tự).", "TooShortPrompt");
        }

        // 3. Chỉ toàn ký tự đặc biệt
        if (prompt.Count(char.IsLetterOrDigit) == 0)
        {
            return (false, "Prompt chỉ chứa ký tự đặc biệt, không có ý nghĩa.", "InvalidFormatPrompt");
        }

        // 4. Quá nhiều ký tự đặc biệt (> 50%)
        var specialCharCount = prompt.Count(c => !char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c));
        var specialCharRatio = (double)specialCharCount / prompt.Length * 100;
        if (specialCharRatio > MaxSpecialCharRatio)
        {
            return (false, $"Prompt chứa quá nhiều ký tự đặc biệt ({specialCharRatio:F0}%).", "TooManySpecialChars");
        }

        // 5. Phát hiện PII
        if (_privacyService.HasSensitiveData(prompt))
        {
            var piiTypes = _privacyService.GetSensitiveDataTypes(prompt);
            return (false, $"Phát hiện thông tin cá nhân: {string.Join(", ", piiTypes)}.", "PIIViolation");
        }

        // ============================================================
        // LEVEL 2: SPAM DETECTION (dùng attempt đã load sẵn, KHÔNG query lại DB
        // để tránh EF Core change tracking bị corrupt → DbUpdateConcurrencyException)
        // ============================================================

        var submissions = attempt.Submissions.ToList();

        // 6. Kiểm tra số lượng prompt trong 1 phút (Rate Limit)
        var oneMinuteAgo = DateTime.UtcNow.AddMinutes(-1);
        var recentSubmissions = submissions
            .Where(s => s.CreatedAt > oneMinuteAgo)
            .Count();

        if (recentSubmissions >= MaxSubmissionsPerMinute)
        {
            return (false,
                $"Bạn đã gửi {recentSubmissions}/{MaxSubmissionsPerMinute} prompt trong 1 phút. Vui lòng chậm lại.",
                "RateLimitExceeded");
        }

        // 7. Kiểm tra prompt trùng lặp chính xác
        var isExactDuplicate = submissions
            .Any(s => s.UserPrompt.Equals(prompt, StringComparison.OrdinalIgnoreCase));

        if (isExactDuplicate)
        {
            return (false, "Bạn đã gửi prompt này rồi. Vui lòng thử nội dung khác.", "DuplicatePrompt");
        }

        // 8. Kiểm tra prompt tương tự (spam với nội dung khác nhau nhẹ)
        var isSimilarDuplicate = submissions
            .Where(s => s.UserPrompt.Length > 10) // Bỏ qua prompt quá ngắn
            .Any(s => CalculateSimilarity(s.UserPrompt, prompt) > SimilarityThreshold);

        if (isSimilarDuplicate)
        {
            return (false,
                "Prompt này rất giống với prompt bạn đã gửi trước đó. Vui lòng thử nội dung mới khác biệt.",
                "SimilarPromptDuplicate");
        }

        // ============================================================
        // LEVEL 3: WARNING - Vẫn submit nhưng cảnh báo
        // ============================================================

        // 9. Prompt hơi ngắn (5-15 ký tự)
        if (prompt.Length < 15)
        {
            return (true, $"Prompt hơi ngắn ({prompt.Length} ký tự).", "TooShortPromptWarning");
        }

        // 10. Ít từ có nghĩa
        var meaningfulWords = prompt
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Count(w => w.Length >= 2);

        if (meaningfulWords < MinMeaningfulWords)
        {
            return (true, "Prompt có ít từ có nghĩa, hãy viết câu đầy đủ hơn.", "MeaninglessPromptWarning");
        }

        // ============================================================
        // LEVEL 4: VALID
        // ============================================================

        return (true, null, null);
    }

    /// <summary>
    /// Tính độ tương đồng giữa 2 chuỗi (Levenshtein Distance)
    /// </summary>
    private double CalculateSimilarity(string str1, string str2)
    {
        if (string.IsNullOrEmpty(str1) || string.IsNullOrEmpty(str2))
            return 0;

        var normalized1 = str1.ToLowerInvariant().Trim();
        var normalized2 = str2.ToLowerInvariant().Trim();

        if (normalized1 == normalized2)
            return 100;

        var maxLength = Math.Max(normalized1.Length, normalized2.Length);
        if (maxLength == 0)
            return 100;

        var distance = LevenshteinDistance(normalized1, normalized2);
        var similarity = (1.0 - (double)distance / maxLength) * 100;

        return Math.Round(similarity, 2);
    }

    /// <summary>
    /// Levenshtein Distance Algorithm
    /// </summary>
    private int LevenshteinDistance(string s, string t)
    {
        var n = s.Length;
        var m = t.Length;
        var d = new int[n + 1, m + 1];

        if (n == 0) return m;
        if (m == 0) return n;

        for (int i = 0; i <= n; d[i, 0] = i++) ;
        for (int j = 0; j <= m; d[0, j] = j++) ;

        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                var cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }

        return d[n, m];
    }
}