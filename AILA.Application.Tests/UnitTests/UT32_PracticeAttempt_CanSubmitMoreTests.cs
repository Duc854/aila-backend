using AILA.Domain.Entities;

namespace AILA.Application.Tests.UnitTests;

/// <summary>
/// Sheet: UT32_CanSubmitMore — <see cref="PracticeAttempt.CanSubmitMore"/>
/// Module: AIPractice · CC = 3 · 7 test case
///
/// Biểu thức: Status == InProgress &amp;&amp; Submissions.Count(s =&gt; !s.IsRejected) &lt; maxAttempts
/// Nhánh: B1 = Status == InProgress · B2 = số lượt HỢP LỆ &lt; maxAttempts
///
/// Điểm nghiệp vụ then chốt: chỉ đếm submission KHÔNG bị reject — người học bị chặn vì
/// vi phạm chính sách không bị trừ lượt.
/// </summary>
public class UT32_PracticeAttempt_CanSubmitMoreTests
{
    private static PracticeAttempt BuildAttempt(int validCount = 0, int rejectedCount = 0)
    {
        var attempt = new PracticeAttempt(Guid.NewGuid(), Guid.NewGuid());

        for (var i = 0; i < validCount; i++)
            attempt.AddSubmission($"Prompt hop le so {i + 1}", "AI response");

        for (var i = 0; i < rejectedCount; i++)
            attempt.AddRejectedSubmission($"Prompt bi tu choi {i + 1}", "Vi pham", "PIIViolation");

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
        var attempt = BuildAttempt(validCount: 2);

        Assert.True(attempt.CanSubmitMore(3));
    }

    /// <summary>UTCID03 · B2=F · Type B — đã dùng đúng 3/3 lượt (biên).</summary>
    [Fact]
    public void UTCID03_ExactlyAtLimit_ReturnsFalse()
    {
        var attempt = BuildAttempt(validCount: 3);

        Assert.False(attempt.CanSubmitMore(3));
    }

    /// <summary>
    /// UTCID04 · B2=T · Type A — 2 lượt hợp lệ + 5 lượt bị từ chối, giới hạn 3.
    /// Lượt bị từ chối KHÔNG bị tính vào hạn mức.
    /// </summary>
    [Fact]
    public void UTCID04_RejectedSubmissionsAreNotCounted_ReturnsTrue()
    {
        var attempt = BuildAttempt(validCount: 2, rejectedCount: 5);

        Assert.Equal(7, attempt.Submissions.Count);
        Assert.True(attempt.CanSubmitMore(3));
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
