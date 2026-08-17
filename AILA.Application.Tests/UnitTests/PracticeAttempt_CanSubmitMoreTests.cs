using AILA.Domain.Entities;

namespace AILA.Application.Tests.UnitTests;

/// <summary>
/// Sheet: UT29_CanSubmitMore — <see cref="PracticeAttempt.CanSubmitMore"/>
/// Module: AIPractice · CC = 3 · 7 test case
///
/// Biểu thức: Status == InProgress &amp;&amp; Submissions.Count &lt; maxAttempts
/// Nhánh: B1 = Status == InProgress · B2 = số lượt đã dùng &lt; maxAttempts
///
/// LƯU Ý THAY ĐỔI NGHIỆP VỤ: bản trước của entity có khái niệm "submission bị reject"
/// (không bị trừ lượt). Khái niệm này ĐÃ BỊ GỠ khỏi <see cref="PromptSubmission"/> —
/// hiện MỌI submission đều bị tính vào hạn mức. UTCID04 phủ đúng hành vi mới này.
/// </summary>
public class UT29_PracticeAttempt_CanSubmitMoreTests
{
    private static PracticeAttempt BuildAttempt(int submissionCount = 0)
    {
        var attempt = new PracticeAttempt(Guid.NewGuid(), Guid.NewGuid());

        for (var i = 0; i < submissionCount; i++)
            attempt.AddSubmission($"Prompt so {i + 1}", "AI response");

        return attempt;
    }

    /// <summary>UTCID01 · B1=T, B2=T · Type N — chưa dùng lượt nào.</summary>
    [Fact]
    public void UTCID01_NoSubmissionYet_ReturnsTrue()
    {
        var attempt = BuildAttempt();

        Assert.True(attempt.CanSubmitMore(3));
    }

    /// <summary>UTCID02 · B2=T · Type N — đã dùng 2/3 lượt.</summary>
    [Fact]
    public void UTCID02_BelowLimit_ReturnsTrue()
    {
        var attempt = BuildAttempt(submissionCount: 2);

        Assert.True(attempt.CanSubmitMore(3));
    }

    /// <summary>UTCID03 · B2=F · Type B — đã dùng đúng 3/3 lượt (biên).</summary>
    [Fact]
    public void UTCID03_ExactlyAtLimit_ReturnsFalse()
    {
        var attempt = BuildAttempt(submissionCount: 3);

        Assert.False(attempt.CanSubmitMore(3));
    }

    /// <summary>
    /// UTCID04 · B2=F · Type A — số lượt đã dùng VƯỢT giới hạn (7 lượt, giới hạn 3).
    /// Xảy ra khi Expert hạ MaxPromptAttempts sau khi học viên đã submit nhiều lượt:
    /// mọi submission đều được tính, không có ngoại lệ nào.
    /// </summary>
    [Fact]
    public void UTCID04_SubmissionsExceedLimit_ReturnsFalse()
    {
        var attempt = BuildAttempt(submissionCount: 7);

        Assert.Equal(7, attempt.Submissions.Count);
        Assert.False(attempt.CanSubmitMore(3));
    }

    /// <summary>UTCID05 · B1=F · Type A — lượt thực hành đã hoàn thành.</summary>
    [Fact]
    public void UTCID05_CompletedAttempt_ReturnsFalse()
    {
        var attempt = BuildAttempt();
        attempt.Complete(80m, "Tot");

        Assert.False(attempt.CanSubmitMore(3));
    }

    /// <summary>UTCID06 · B1=F · Type A — lượt thực hành đã bị bỏ dở.</summary>
    [Fact]
    public void UTCID06_AbandonedAttempt_ReturnsFalse()
    {
        var attempt = BuildAttempt();
        attempt.Abandon();

        Assert.False(attempt.CanSubmitMore(3));
    }

    /// <summary>UTCID07 · B2=F · Type B — maxAttempts = 0 (biên dưới): không được submit lượt nào.</summary>
    [Fact]
    public void UTCID07_MaxAttemptsZero_ReturnsFalse()
    {
        var attempt = BuildAttempt();

        Assert.False(attempt.CanSubmitMore(0));
    }
}
