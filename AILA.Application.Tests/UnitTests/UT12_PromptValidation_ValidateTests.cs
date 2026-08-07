using AILA.Application.Common.Interfaces.AI;
using AILA.Domain.Entities;
using AILA.Infrastructure.Services.AI;
using Moq;

namespace AILA.Application.Tests.UnitTests;

/// <summary>
/// Sheet: UT12_ValidatePrompt — <see cref="PromptValidationService.ValidateAsync"/>
/// Module: AIPractice · CC = 16 · 17 test case
///
/// 4 mức xử lý: LEVEL 1 REJECT (B1–B5) · LEVEL 2 SPAM (B7, B9, B12)
///              LEVEL 3 WARNING vẫn hợp lệ (B13, B15) · LEVEL 4 VALID
///
/// Hằng số: MinLength = 5 · MinMeaningfulWords = 2 · MaxSpecialCharRatio = 50
///          MaxSubmissionsPerMinute = 20 · SimilarityThreshold = 80
/// </summary>
public class UT12_PromptValidation_ValidateTests
{
    private const string ValidPrompt = "Hay viet mot email cam on khach hang da mua san pham";

    private readonly Mock<IPrivacyService> _privacy = new();
    private readonly PracticeAttempt _attempt = new(Guid.NewGuid(), Guid.NewGuid());

    public UT12_PromptValidation_ValidateTests()
    {
        _privacy.Setup(x => x.HasSensitiveData(It.IsAny<string>())).Returns(false);
    }

    private PromptValidationService CreateSut() => new(_privacy.Object);

    private Task<(bool IsValid, string? ViolationReason, string? PolicyName)> Act(string prompt) =>
        CreateSut().ValidateAsync(prompt, _attempt, CancellationToken.None);

    /// <summary>Thêm n submission "ngắn" (≤ 10 ký tự) để không ảnh hưởng kiểm tra similarity (B10).</summary>
    private void AddShortSubmissions(int count)
    {
        for (var i = 0; i < count; i++)
            _attempt.AddSubmission($"old {i}", "ai response");
    }

    /// <summary>UTCID01 · B1=T · Type A — prompt null.</summary>
    [Fact]
    public async Task UTCID01_NullPrompt_ReturnsEmptyPrompt()
    {
        var (isValid, reason, policy) = await Act(null!);

        Assert.False(isValid);
        Assert.Equal("Nội dung prompt không được để trống.", reason);
        Assert.Equal("EmptyPrompt", policy);
    }

    /// <summary>UTCID02 · B1=T · Type A — prompt toàn khoảng trắng.</summary>
    [Fact]
    public async Task UTCID02_WhitespacePrompt_ReturnsEmptyPrompt()
    {
        var (isValid, reason, policy) = await Act("   ");

        Assert.False(isValid);
        Assert.Equal("Nội dung prompt không được để trống.", reason);
        Assert.Equal("EmptyPrompt", policy);
    }

    /// <summary>UTCID03 · B2=T · Type B — 4 ký tự (biên dưới không hợp lệ của MinLength = 5).</summary>
    [Fact]
    public async Task UTCID03_PromptLength4_ReturnsTooShortPrompt()
    {
        var (isValid, reason, policy) = await Act("abcd");

        Assert.False(isValid);
        Assert.Equal("Prompt quá ngắn (cần ít nhất 5 ký tự).", reason);
        Assert.Equal("TooShortPrompt", policy);
    }

    /// <summary>
    /// UTCID04 · B2=F, B3=T · Type B — toàn ký tự đặc biệt, dài ĐÚNG 5.
    /// Điểm white-box then chốt: nếu dùng "@@@" (3 ký tự) thì B2 sẽ che mất B3
    /// và nhánh InvalidFormatPrompt KHÔNG BAO GIỜ được phủ (đúng cảnh báo spec §8.6).
    /// </summary>
    [Fact]
    public async Task UTCID04_OnlySpecialCharsWithLength5_ReturnsInvalidFormatPrompt()
    {
        var (isValid, reason, policy) = await Act("@@@@@");

        Assert.False(isValid);
        Assert.Equal("Prompt chỉ chứa ký tự đặc biệt, không có ý nghĩa.", reason);
        Assert.Equal("InvalidFormatPrompt", policy);
    }

    /// <summary>UTCID05 · B4=T · Type A — 4/6 ký tự đặc biệt = 66,67 % &gt; 50 %.</summary>
    [Fact]
    public async Task UTCID05_SpecialCharRatioAbove50_ReturnsTooManySpecialChars()
    {
        var (isValid, reason, policy) = await Act("ab@#$%");

        Assert.False(isValid);
        Assert.Equal("Prompt chứa quá nhiều ký tự đặc biệt (67%).", reason);
        Assert.Equal("TooManySpecialChars", policy);
    }

    /// <summary>UTCID06 · B4=F · Type B — tỉ lệ ĐÚNG 50 % ⇒ vẫn hợp lệ (toán tử &gt;), rơi xuống Level 3.</summary>
    [Fact]
    public async Task UTCID06_SpecialCharRatioExactly50_PassesRejectLevel()
    {
        var (isValid, reason, policy) = await Act("abc@#$");

        Assert.True(isValid);
        Assert.Equal("Prompt hơi ngắn (6 ký tự).", reason);
        Assert.Equal("TooShortPromptWarning", policy);
    }

    /// <summary>UTCID07 · B5=T · Type A — phát hiện thông tin cá nhân.</summary>
    [Fact]
    public async Task UTCID07_PromptWithPii_ReturnsPiiViolation()
    {
        const string prompt = "Email cua toi la abc@gmail.com";
        _privacy.Setup(x => x.HasSensitiveData(prompt)).Returns(true);
        _privacy.Setup(x => x.GetSensitiveDataTypes(prompt)).Returns(new List<string> { "Email" });

        var (isValid, reason, policy) = await Act(prompt);

        Assert.False(isValid);
        Assert.Equal("Phát hiện thông tin cá nhân: Email.", reason);
        Assert.Equal("PIIViolation", policy);
    }

    /// <summary>UTCID08 · B7=T · Type B — 20 submission trong 1 phút (ĐÚNG ngưỡng, toán tử &gt;=).</summary>
    [Fact]
    public async Task UTCID08_TwentySubmissionsInOneMinute_ReturnsRateLimitExceeded()
    {
        AddShortSubmissions(20);

        var (isValid, reason, policy) = await Act(ValidPrompt);

        Assert.False(isValid);
        Assert.Equal("Bạn đã gửi 20/20 prompt trong 1 phút. Vui lòng chậm lại.", reason);
        Assert.Equal("RateLimitExceeded", policy);
    }

    /// <summary>UTCID09 · B7=F · Type B — 19 submission (ngưỡng − 1) ⇒ vẫn cho gửi.</summary>
    [Fact]
    public async Task UTCID09_NineteenSubmissionsInOneMinute_IsAllowed()
    {
        AddShortSubmissions(19);

        var (isValid, reason, policy) = await Act(ValidPrompt);

        Assert.True(isValid);
        Assert.Null(reason);
        Assert.Null(policy);
    }

    /// <summary>UTCID10 · B9=T · Type A — trùng chính xác, không phân biệt hoa thường.</summary>
    [Fact]
    public async Task UTCID10_ExactDuplicateIgnoringCase_ReturnsDuplicatePrompt()
    {
        _attempt.AddSubmission("HAY VIET MOT EMAIL CAM ON KHACH HANG", "ai response");

        var (isValid, reason, policy) = await Act("Hay viet mot email cam on khach hang");

        Assert.False(isValid);
        Assert.Equal("Bạn đã gửi prompt này rồi. Vui lòng thử nội dung khác.", reason);
        Assert.Equal("DuplicatePrompt", policy);
    }

    /// <summary>UTCID11 · B12=T · Type A — độ tương tự ~97 % &gt; 80 %.</summary>
    [Fact]
    public async Task UTCID11_SimilarPromptAboveThreshold_ReturnsSimilarPromptDuplicate()
    {
        _attempt.AddSubmission("Hay viet mot email cam on khach hang!", "ai response");

        var (isValid, reason, policy) = await Act("Hay viet mot email cam on khach hang");

        Assert.False(isValid);
        Assert.Equal(
            "Prompt này rất giống với prompt bạn đã gửi trước đó. Vui lòng thử nội dung mới khác biệt.",
            reason);
        Assert.Equal("SimilarPromptDuplicate", policy);
    }

    /// <summary>
    /// UTCID12 · B11=F · Type B — độ tương tự ĐÚNG 80,00 % ⇒ KHÔNG chặn (toán tử &gt; 80).
    /// distance = 4, maxLength = 20 ⇒ (1 − 4/20) × 100 = 80,00.
    /// </summary>
    [Fact]
    public async Task UTCID12_SimilarityExactly80_IsAllowed()
    {
        _attempt.AddSubmission("aaaaa bbbb cccc dddd", "ai response");

        var (isValid, reason, policy) = await Act("aaaaa bbbb cccc XXXX");

        Assert.True(isValid);
        Assert.Null(reason);
        Assert.Null(policy);
    }

    /// <summary>
    /// UTCID13 · B10 lọc bỏ, B13=T · Type B — submission cũ dài ĐÚNG 10 ký tự
    /// ⇒ bị Where(s =&gt; s.UserPrompt.Length &gt; 10) loại khỏi kiểm tra similarity.
    /// </summary>
    [Fact]
    public async Task UTCID13_PreviousSubmissionWithLength10_IsExcludedFromSimilarityCheck()
    {
        _attempt.AddSubmission("abcdefghij", "ai response");

        var (isValid, reason, policy) = await Act("abcdefghiX");

        Assert.True(isValid);
        Assert.Equal("Prompt hơi ngắn (10 ký tự).", reason);
        Assert.Equal("TooShortPromptWarning", policy);
    }

    /// <summary>UTCID14 · B2=F · Type B — dài ĐÚNG 5 ký tự (biên dưới hợp lệ của MinLength).</summary>
    [Fact]
    public async Task UTCID14_PromptLengthExactly5_PassesMinLengthCheck()
    {
        var (isValid, reason, policy) = await Act("abcde");

        Assert.True(isValid);
        Assert.Equal("Prompt hơi ngắn (5 ký tự).", reason);
        Assert.Equal("TooShortPromptWarning", policy);
    }

    /// <summary>UTCID15 · B13=T · Type B — dài ĐÚNG 14 ký tự (biên dưới của cảnh báo &lt; 15).</summary>
    [Fact]
    public async Task UTCID15_PromptLengthExactly14_ReturnsTooShortWarning()
    {
        var (isValid, reason, policy) = await Act("abcd efgh ijkl");

        Assert.True(isValid);
        Assert.Equal("Prompt hơi ngắn (14 ký tự).", reason);
        Assert.Equal("TooShortPromptWarning", policy);
    }

    /// <summary>
    /// UTCID16 · B13=F, B15=T · Type B — dài ĐÚNG 15 để thoát B13, chỉ có 1 từ để chạm B15.
    /// Nếu prompt ngắn hơn 15 thì B13 return sớm và B15 KHÔNG BAO GIỜ được phủ.
    /// </summary>
    [Fact]
    public async Task UTCID16_SingleWordWithLength15_ReturnsMeaninglessWarning()
    {
        var (isValid, reason, policy) = await Act("aaaaaaaaaaaaaaa");

        Assert.True(isValid);
        Assert.Equal("Prompt có ít từ có nghĩa, hãy viết câu đầy đủ hơn.", reason);
        Assert.Equal("MeaninglessPromptWarning", policy);
    }

    /// <summary>UTCID17 · Toàn bộ nhánh = F · Type N — prompt hợp lệ hoàn toàn.</summary>
    [Fact]
    public async Task UTCID17_FullyValidPrompt_ReturnsValidWithoutWarning()
    {
        var (isValid, reason, policy) = await Act(ValidPrompt);

        Assert.True(isValid);
        Assert.Null(reason);
        Assert.Null(policy);
    }
}
