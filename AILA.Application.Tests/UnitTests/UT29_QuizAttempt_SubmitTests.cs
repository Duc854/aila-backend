using AILA.Domain.Entities;
using AILA.Domain.Enums;

namespace AILA.Application.Tests.UnitTests;

/// <summary>
/// Sheet: UT29_QuizAttemptSubmit — <see cref="QuizAttempt.Submit"/>
/// Module: Quiz · CC = 4 · 7 test case
///
/// Nhánh: B1 = Status == Submitted (throw) · B2 = score &lt; 0 · B3 = score &gt; 100
/// Miền điểm hợp lệ [0 .. 100] ⇒ BVA đầy đủ: −0.01 / 0 / 100 / 100.01.
/// Đây là hàng rào cuối bảo vệ bất biến "một lượt làm bài chỉ được nộp một lần".
/// </summary>
public class UT29_QuizAttempt_SubmitTests
{
    private static QuizAttempt BuildAttempt() => new(Guid.NewGuid(), Guid.NewGuid());

    /// <summary>UTCID01 · Toàn bộ nhánh = F · Type N — nộp bài đạt.</summary>
    [Fact]
    public void UTCID01_ValidScorePassed_SetsSubmittedState()
    {
        var attempt = BuildAttempt();

        attempt.Submit(66.67m, isPassed: true);

        Assert.Equal(66.67m, attempt.Score);
        Assert.True(attempt.IsPassed);
        Assert.Equal(QuizAttemptStatus.Submitted, attempt.Status);
        Assert.NotNull(attempt.SubmittedAt);
        Assert.NotNull(attempt.UpdatedAt);
    }

    /// <summary>UTCID02 · B1=T · Type A — nộp lần thứ hai.</summary>
    [Fact]
    public void UTCID02_AlreadySubmitted_ThrowsInvalidOperation()
    {
        var attempt = BuildAttempt();
        attempt.Submit(50m, isPassed: false);

        var ex = Assert.Throws<InvalidOperationException>(() => attempt.Submit(90m, true));

        Assert.Equal("Bài kiểm tra đã được nộp.", ex.Message);
        Assert.Equal(50m, attempt.Score);
    }

    /// <summary>UTCID03 · B2=T · Type A — điểm âm (biên dưới không hợp lệ).</summary>
    [Fact]
    public void UTCID03_NegativeScore_ThrowsArgumentException()
    {
        var attempt = BuildAttempt();

        var ex = Assert.Throws<ArgumentException>(() => attempt.Submit(-0.01m, false));

        Assert.Contains("Điểm số phải nằm trong khoảng từ 0 đến 100.", ex.Message);
        Assert.Equal(QuizAttemptStatus.InProgress, attempt.Status);
    }

    /// <summary>UTCID04 · B2=F, B3=T · Type B — điểm vượt 100 (biên trên không hợp lệ).</summary>
    [Fact]
    public void UTCID04_ScoreAbove100_ThrowsArgumentException()
    {
        var attempt = BuildAttempt();

        Assert.Throws<ArgumentException>(() => attempt.Submit(100.01m, true));
        Assert.Equal(QuizAttemptStatus.InProgress, attempt.Status);
    }

    /// <summary>UTCID05 · B2=F · Type B — điểm 0 (biên dưới hợp lệ).</summary>
    [Fact]
    public void UTCID05_ScoreExactlyZero_Succeeds()
    {
        var attempt = BuildAttempt();

        attempt.Submit(0m, isPassed: false);

        Assert.Equal(0m, attempt.Score);
        Assert.Equal(QuizAttemptStatus.Submitted, attempt.Status);
    }

    /// <summary>UTCID06 · B3=F · Type B — điểm 100 (biên trên hợp lệ).</summary>
    [Fact]
    public void UTCID06_ScoreExactly100_Succeeds()
    {
        var attempt = BuildAttempt();

        attempt.Submit(100m, isPassed: true);

        Assert.Equal(100m, attempt.Score);
        Assert.True(attempt.IsPassed);
    }

    /// <summary>UTCID07 · Toàn bộ nhánh = F · Type N — nộp bài trượt vẫn được ghi nhận Submitted.</summary>
    [Fact]
    public void UTCID07_FailedAttempt_IsStillMarkedSubmitted()
    {
        var attempt = BuildAttempt();

        attempt.Submit(33.33m, isPassed: false);

        Assert.Equal(33.33m, attempt.Score);
        Assert.False(attempt.IsPassed);
        Assert.Equal(QuizAttemptStatus.Submitted, attempt.Status);
    }
}
