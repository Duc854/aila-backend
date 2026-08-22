using AILA.Application.Common.InternalServices;
using AILA.Application.Common.Interfaces;
using AILA.Application.Common.Interfaces.Repositories;
using AILA.Domain.Constants;
using AILA.Domain.Entities;
using Moq;

namespace AILA.Application.Tests.UnitTests;

/// <summary>
/// Sheet: UT39_UpdateBehaviorScore — <see cref="LearnerBehaviorService.IncreaseScoreAsync"/>
/// Module: Recommendation · CC = 5 · 11 test case
///
/// Trọng số hành vi (PRD 3.0): Enroll +20 · Complete course +100 ·
/// AI Practice +30 · Perfect quiz +30. BehaviorScore bị chặn trần 1000.
///
/// Nhánh: B1 = score &lt;= 0 (ném lỗi) · B2 = danh sách tag rỗng (thoát sớm)
///        B3 = tag chưa có LearnerTagScore ⇒ tạo mới rồi cộng
///        B4 = tag đã có LearnerTagScore ⇒ cộng dồn, KHÔNG tạo bản ghi mới
///        B5 = cộng vượt trần 1000 ⇒ giữ nguyên 1000
/// </summary>
public class UT39_LearnerBehaviorService_IncreaseScoreTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ILearnerTagScoreRepository> _scoreRepository = new();
    private readonly Guid _learnerId = Guid.NewGuid();

    public UT39_LearnerBehaviorService_IncreaseScoreTests()
    {
        _unitOfWork.SetupGet(x => x.LearnerTagScores).Returns(_scoreRepository.Object);
        WithExistingScores();
    }

    private void WithExistingScores(params LearnerTagScore[] scores)
    {
        _scoreRepository
            .Setup(x => x.GetByLearnerIdAndTagIdsAsync(
                It.IsAny<Guid>(),
                It.IsAny<IEnumerable<Guid>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(scores.ToList());
    }

    private LearnerBehaviorService CreateSut() => new(_unitOfWork.Object);

    private static Tag NewTag(string name) => Tag.CreateByAdmin(name, name);

    private LearnerTagScore ExistingScore(Guid tagId, int profileSeed, int behaviorScore)
    {
        var score = new LearnerTagScore(_learnerId, tagId, profileSeed);
        if (behaviorScore > 0)
            score.IncreaseBehaviorScore(behaviorScore);
        return score;
    }

    /// <summary>
    /// UTCID01 · B3=T · Type N — Enroll khóa học có 2 tag, học viên chưa từng có điểm cho 2 tag đó.
    /// Kỳ vọng: tạo 2 LearnerTagScore mới, mỗi bản ghi BehaviorScore = 20.
    /// </summary>
    [Fact]
    public async Task UTCID01_NewTags_CreatesScoresWithEnrollWeight()
    {
        var tags = new List<Tag> { NewTag("ai"), NewTag("prompt") };
        var added = new List<LearnerTagScore>();

        _scoreRepository
            .Setup(x => x.AddAsync(It.IsAny<LearnerTagScore>()))
            .Callback<LearnerTagScore>(added.Add)
            .Returns(Task.CompletedTask);

        await CreateSut().IncreaseScoreAsync(
            _learnerId, tags, BehaviorScoreConstants.EnrollCourse);

        Assert.Equal(2, added.Count);
        Assert.All(added, s => Assert.Equal(20, s.BehaviorScore));
        Assert.All(added, s => Assert.Equal(0, s.ProfileSeed));
        Assert.All(added, s => Assert.Equal(20, s.RawScore));
    }

    /// <summary>
    /// UTCID02 · B4=T · Type N — tag đã có seed 200 và 20 điểm hành vi (đã Enroll),
    /// nay Complete course (+100) ⇒ BehaviorScore 120, RawScore 320, không tạo bản ghi mới.
    /// </summary>
    [Fact]
    public async Task UTCID02_ExistingTag_AccumulatesCompleteCourseWeight()
    {
        var tag = NewTag("prompt");
        var existing = ExistingScore(tag.Id, profileSeed: 200, behaviorScore: 20);
        WithExistingScores(existing);

        await CreateSut().IncreaseScoreAsync(
            _learnerId, new List<Tag> { tag }, BehaviorScoreConstants.CompleteCourse);

        Assert.Equal(120, existing.BehaviorScore);
        Assert.Equal(320, existing.RawScore);
        _scoreRepository.Verify(x => x.AddAsync(It.IsAny<LearnerTagScore>()), Times.Never);
    }

    /// <summary>UTCID03 · B4=T · Type N — hoàn thành AI Practice (+30) trên tag chưa có điểm hành vi.</summary>
    [Fact]
    public async Task UTCID03_ExistingTag_AppliesAiPracticeWeight()
    {
        var tag = NewTag("prompt");
        var existing = ExistingScore(tag.Id, profileSeed: 200, behaviorScore: 0);
        WithExistingScores(existing);

        await CreateSut().IncreaseScoreAsync(
            _learnerId, new List<Tag> { tag }, BehaviorScoreConstants.CompleteAIPractice);

        Assert.Equal(30, existing.BehaviorScore);
        Assert.Equal(230, existing.RawScore);
    }

    /// <summary>UTCID04 · B4=T · Type N — đạt điểm tuyệt đối quiz (+30) cộng dồn lên 30 điểm sẵn có.</summary>
    [Fact]
    public async Task UTCID04_ExistingTag_AppliesPerfectQuizWeight()
    {
        var tag = NewTag("prompt");
        var existing = ExistingScore(tag.Id, profileSeed: 0, behaviorScore: 30);
        WithExistingScores(existing);

        await CreateSut().IncreaseScoreAsync(
            _learnerId, new List<Tag> { tag }, BehaviorScoreConstants.PerfectQuiz);

        Assert.Equal(60, existing.BehaviorScore);
        Assert.Equal(60, existing.RawScore);
    }

    /// <summary>UTCID05 · B1=T · Type B — score = 0 (biên dưới không hợp lệ), không được chạm repository.</summary>
    [Fact]
    public async Task UTCID05_ZeroScore_ThrowsAndTouchesNoRepository()
    {
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            CreateSut().IncreaseScoreAsync(_learnerId, new List<Tag> { NewTag("ai") }, 0));

        Assert.Contains("Behavior score phải lớn hơn 0.", ex.Message);
        _scoreRepository.Verify(x => x.GetByLearnerIdAndTagIdsAsync(
            It.IsAny<Guid>(), It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>UTCID06 · B1=T · Type A — score âm.</summary>
    [Fact]
    public async Task UTCID06_NegativeScore_ThrowsArgumentException()
    {
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            CreateSut().IncreaseScoreAsync(_learnerId, new List<Tag> { NewTag("ai") }, -20));

        Assert.Equal("score", ex.ParamName);
    }

    /// <summary>UTCID07 · B1=F · Type B — score = 1 (biên dưới hợp lệ nhỏ nhất).</summary>
    [Fact]
    public async Task UTCID07_MinimumValidScore_IsApplied()
    {
        var tag = NewTag("prompt");
        var existing = ExistingScore(tag.Id, profileSeed: 0, behaviorScore: 0);
        WithExistingScores(existing);

        await CreateSut().IncreaseScoreAsync(_learnerId, new List<Tag> { tag }, 1);

        Assert.Equal(1, existing.BehaviorScore);
    }

    /// <summary>UTCID08 · B5=F · Type B — 900 + 100 = đúng trần 1000, không bị cắt.</summary>
    [Fact]
    public async Task UTCID08_ReachesCapExactly_KeepsOneThousand()
    {
        var tag = NewTag("prompt");
        var existing = ExistingScore(tag.Id, profileSeed: 200, behaviorScore: 900);
        WithExistingScores(existing);

        await CreateSut().IncreaseScoreAsync(
            _learnerId, new List<Tag> { tag }, BehaviorScoreConstants.CompleteCourse);

        Assert.Equal(1000, existing.BehaviorScore);
        Assert.Equal(1200, existing.RawScore);
    }

    /// <summary>UTCID09 · B5=T · Type B — đã chạm trần 1000, cộng thêm 30 vẫn phải giữ 1000.</summary>
    [Fact]
    public async Task UTCID09_AboveCap_StaysAtOneThousand()
    {
        var tag = NewTag("prompt");
        var existing = ExistingScore(tag.Id, profileSeed: 200, behaviorScore: 1000);
        WithExistingScores(existing);

        await CreateSut().IncreaseScoreAsync(
            _learnerId, new List<Tag> { tag }, BehaviorScoreConstants.PerfectQuiz);

        Assert.Equal(1000, existing.BehaviorScore);
        Assert.Equal(1200, existing.RawScore);
    }

    /// <summary>UTCID10 · B2=T · Type B — khóa học không gắn tag nào ⇒ thoát sớm, không truy vấn.</summary>
    [Fact]
    public async Task UTCID10_EmptyTagList_DoesNothing()
    {
        await CreateSut().IncreaseScoreAsync(
            _learnerId, new List<Tag>(), BehaviorScoreConstants.EnrollCourse);

        _scoreRepository.Verify(x => x.GetByLearnerIdAndTagIdsAsync(
            It.IsAny<Guid>(), It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()), Times.Never);
        _scoreRepository.Verify(x => x.AddAsync(It.IsAny<LearnerTagScore>()), Times.Never);
    }

    /// <summary>UTCID11 · B3=T · Type A — learnerId rỗng, bản ghi mới phải bị chặn ngay tại Domain.</summary>
    [Fact]
    public async Task UTCID11_EmptyLearnerId_ThrowsArgumentException()
    {
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            CreateSut().IncreaseScoreAsync(
                Guid.Empty, new List<Tag> { NewTag("ai") }, BehaviorScoreConstants.EnrollCourse));

        Assert.Contains("Mã người học không hợp lệ.", ex.Message);
    }
}
