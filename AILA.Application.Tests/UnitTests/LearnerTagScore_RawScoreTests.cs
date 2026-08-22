using AILA.Domain.Constants;
using AILA.Domain.Entities;

namespace AILA.Application.Tests.UnitTests;

/// <summary>
/// Sheet: UT38_CalculateRawScore — <see cref="LearnerTagScore.RawScore"/>
/// Module: Recommendation · CC = 4 · 9 test case
///
/// Công thức TDS: RawScore = ProfileSeed + BehaviorScore.
/// ProfileSeed ∈ [0, 200] (200 = seed cấp khi hoàn thành onboarding),
/// BehaviorScore ∈ [0, 1000] ⇒ RawScore ∈ [0, 1200] = MaxTagScore.
///
/// Nhánh: B1 = learnerId rỗng · B2 = tagId rỗng
///        B3 = profileSeed &lt; 0 · B4 = profileSeed &gt; 200 (MaxProfileSeed)
/// </summary>
public class UT38_LearnerTagScore_RawScoreTests
{
    private static readonly Guid LearnerId = Guid.NewGuid();
    private static readonly Guid TagId = Guid.NewGuid();

    /// <summary>UTCID01 · Toàn bộ nhánh = F · Type N — vừa onboarding xong, chưa có hành vi nào.</summary>
    [Fact]
    public void UTCID01_ProfileSeedOnly_RawScoreEqualsProfileSeed()
    {
        var score = new LearnerTagScore(LearnerId, TagId, profileSeed: 200);

        Assert.Equal(200, score.ProfileSeed);
        Assert.Equal(0, score.BehaviorScore);
        Assert.Equal(200, score.RawScore);
    }

    /// <summary>UTCID02 · Toàn bộ nhánh = F · Type B — tag mới sinh từ hành vi, chưa có seed (biên dưới = 0).</summary>
    [Fact]
    public void UTCID02_NoSeedNoBehavior_RawScoreIsZero()
    {
        var score = new LearnerTagScore(LearnerId, TagId);

        Assert.Equal(0, score.ProfileSeed);
        Assert.Equal(0, score.BehaviorScore);
        Assert.Equal(0, score.RawScore);
    }

    /// <summary>
    /// UTCID03 · Toàn bộ nhánh = F · Type B — biên trên tuyệt đối:
    /// ProfileSeed 200 (max) + BehaviorScore 1000 (max) = 1200 = MaxTagScore.
    /// </summary>
    [Fact]
    public void UTCID03_MaxSeedAndMaxBehavior_RawScoreEqualsMaxTagScore()
    {
        var score = new LearnerTagScore(LearnerId, TagId, profileSeed: 200);
        score.IncreaseBehaviorScore(1000);

        Assert.Equal(1000, score.BehaviorScore);
        Assert.Equal(1200, score.RawScore);
        Assert.Equal(RecommendationConstants.MaxTagScore, score.RawScore);
    }

    /// <summary>
    /// UTCID04 · Toàn bộ nhánh = F · Type N — persona của Test Gap Brief:
    /// onboarding (200) → Enroll khóa A (+20) → Complete khóa A (+100) ⇒ RawScore = 320.
    /// </summary>
    [Fact]
    public void UTCID04_SeedPlusEnrollPlusComplete_RawScoreIs320()
    {
        var score = new LearnerTagScore(LearnerId, TagId, profileSeed: 200);

        score.IncreaseBehaviorScore(BehaviorScoreConstants.EnrollCourse);   // +20
        score.IncreaseBehaviorScore(BehaviorScoreConstants.CompleteCourse); // +100

        Assert.Equal(120, score.BehaviorScore);
        Assert.Equal(320, score.RawScore);
    }

    /// <summary>UTCID05 · B3=T · Type B — profileSeed = -1 (ngay dưới biên dưới hợp lệ).</summary>
    [Fact]
    public void UTCID05_NegativeProfileSeed_ThrowsArgumentOutOfRange()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new LearnerTagScore(LearnerId, TagId, profileSeed: -1));

        Assert.Equal("profileSeed", ex.ParamName);
        Assert.Contains("Điểm hồ sơ tối đa là 200.", ex.Message);
    }

    /// <summary>UTCID06 · B4=T · Type B — profileSeed = 201 (ngay trên biên trên hợp lệ).</summary>
    [Fact]
    public void UTCID06_ProfileSeedAboveMax_ThrowsArgumentOutOfRange()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new LearnerTagScore(LearnerId, TagId, profileSeed: 201));

        Assert.Equal("profileSeed", ex.ParamName);
    }

    /// <summary>UTCID07 · B1=T · Type A — learnerId rỗng.</summary>
    [Fact]
    public void UTCID07_EmptyLearnerId_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new LearnerTagScore(Guid.Empty, TagId, profileSeed: 200));

        Assert.Contains("Mã người học không hợp lệ.", ex.Message);
    }

    /// <summary>UTCID08 · B1=F, B2=T · Type A — tagId rỗng.</summary>
    [Fact]
    public void UTCID08_EmptyTagId_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new LearnerTagScore(LearnerId, Guid.Empty, profileSeed: 200));

        Assert.Contains("Mã tag không hợp lệ.", ex.Message);
    }

    /// <summary>
    /// UTCID09 · Toàn bộ nhánh = F · Type B — học viên gỡ chủ đề khỏi hồ sơ:
    /// UpdateProfileSeed(0) phải hạ RawScore về đúng phần BehaviorScore đã tích lũy.
    /// </summary>
    [Fact]
    public void UTCID09_ResetProfileSeedToZero_RawScoreKeepsBehaviorOnly()
    {
        var score = new LearnerTagScore(LearnerId, TagId, profileSeed: 200);
        score.IncreaseBehaviorScore(120);

        score.UpdateProfileSeed(0);

        Assert.Equal(0, score.ProfileSeed);
        Assert.Equal(120, score.BehaviorScore);
        Assert.Equal(120, score.RawScore);
    }
}
