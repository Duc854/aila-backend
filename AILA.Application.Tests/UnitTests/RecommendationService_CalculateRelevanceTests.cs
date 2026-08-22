using AILA.Application.Common.InternalServices;
using AILA.Application.Tests.UnitTests.TestHelpers;

namespace AILA.Application.Tests.UnitTests;

/// <summary>
/// Sheet: UT41_CalculateRelevanceScore — <see cref="RecommendationService.GetRecommendationsAsync"/> (bước 4 — tính relevance)
/// Module: Recommendation · CC = 4 · 9 test case
///
/// Công thức TDS: RelevanceScore = Σ(MatchingTagScores) / NumberOfCourseTags, làm tròn 4 chữ số.
/// Mẫu số là TỔNG SỐ TAG CỦA KHÓA HỌC (không phải số tag khớp) — khóa học gắn nhiều tag
/// lạc đề sẽ bị pha loãng điểm, đây là ý đồ nghiệp vụ cần khóa lại bằng test.
///
/// Hồ sơ học viên dùng chung cho mọi test case (điểm đã chuẩn hóa):
///   ai        RawScore 600  ⇒ 0.5000
///   prompt    RawScore 300  ⇒ 0.2500
///   office    RawScore 200  ⇒ 0.1667
///   automation RawScore 1200 ⇒ 1.0000
///
/// Nhánh: B1 = khóa học không có tag nào (Tags.Count = 0) ⇒ điểm 0
///        B2 = tag của khóa học khớp hồ sơ ⇒ cộng điểm + ghi MatchedTags
///        B3 = tag không khớp ⇒ bỏ qua nhưng VẪN tính vào mẫu số
///        B4 = tổng điểm = 0 ⇒ khóa học bị loại khỏi kết quả
/// </summary>
public class UT41_RecommendationService_CalculateRelevanceTests
{
    private readonly RecommendationHarness _harness = new();

    private readonly Guid _ai = Guid.NewGuid();
    private readonly Guid _prompt = Guid.NewGuid();
    private readonly Guid _office = Guid.NewGuid();
    private readonly Guid _automation = Guid.NewGuid();

    public UT41_RecommendationService_CalculateRelevanceTests()
    {
        _harness.WithLearnerScores(
            _harness.Score(_ai, 600),
            _harness.Score(_prompt, 300),
            _harness.Score(_office, 200),
            _harness.Score(_automation, 1200));
    }

    private async Task<List<AILA.Application.Common.Dtos.Recommendation.CourseRecommendationDto>>
        RecommendAsync(params (Guid Id, string Name)[] courseTags)
    {
        _harness.WithCourses(RecommendationHarness.Course("Khóa học X", courseTags));

        return await _harness.CreateSut()
            .GetRecommendationsAsync(_harness.LearnerId);
    }

    /// <summary>
    /// UTCID01 · B2=T · Type N — khóa học 2 tag đều khớp: (0.5000 + 0.2500) / 2 = 0.3750.
    /// </summary>
    [Fact]
    public async Task UTCID01_AllCourseTagsMatched_ReturnsAverageOfMatchedScores()
    {
        var result = await RecommendAsync((_ai, "ai"), (_prompt, "prompt"));

        var course = Assert.Single(result);
        Assert.Equal(0.375m, course.RecommendationScore);
        Assert.Equal(new[] { "ai", "prompt" }, course.MatchedTags);
    }

    /// <summary>
    /// UTCID02 · B3=T · Type N — khóa học 2 tag, chỉ 1 tag khớp:
    /// (0.5000 + 0) / 2 = 0.2500 — tag lạc đề vẫn nằm ở mẫu số.
    /// </summary>
    [Fact]
    public async Task UTCID02_PartiallyMatchedTags_UnmatchedTagStillCountsInDenominator()
    {
        var result = await RecommendAsync((_ai, "ai"), (Guid.NewGuid(), "python"));

        var course = Assert.Single(result);
        Assert.Equal(0.25m, course.RecommendationScore);
        Assert.Equal(new[] { "ai" }, course.MatchedTags);
    }

    /// <summary>UTCID03 · B2=T · Type B — khóa học đúng 1 tag (mẫu số nhỏ nhất): 0.2500 / 1 = 0.2500.</summary>
    [Fact]
    public async Task UTCID03_SingleTagCourse_ScoreEqualsThatTagScore()
    {
        var result = await RecommendAsync((_prompt, "prompt"));

        Assert.Equal(0.25m, Assert.Single(result).RecommendationScore);
    }

    /// <summary>
    /// UTCID04 · B1=T · Type B — khóa học chưa gắn tag nào (mẫu số = 0):
    /// phải trả về điểm 0 và bị loại, tuyệt đối không được chia cho 0.
    /// </summary>
    [Fact]
    public async Task UTCID04_CourseWithoutTags_IsExcludedWithoutDivideByZero()
    {
        var result = await RecommendAsync(Array.Empty<(Guid, string)>());

        Assert.Empty(result);
    }

    /// <summary>UTCID05 · B4=T · Type A — khóa học 3 tag nhưng không tag nào khớp hồ sơ ⇒ điểm 0 ⇒ bị loại.</summary>
    [Fact]
    public async Task UTCID05_NoTagMatched_CourseIsExcluded()
    {
        var result = await RecommendAsync(
            (Guid.NewGuid(), "python"),
            (Guid.NewGuid(), "machine-learning"),
            (Guid.NewGuid(), "data-engineering"));

        Assert.Empty(result);
    }

    /// <summary>
    /// UTCID06 · B2=T · Type B — biên trên: khóa học 1 tag mà học viên đạt điểm kịch trần
    /// (RawScore 1200 ⇒ 1.0000) ⇒ RelevanceScore = 1.0000, không thể vượt quá 1.
    /// </summary>
    [Fact]
    public async Task UTCID06_MaxScoredTag_RelevanceCapsAtOne()
    {
        var result = await RecommendAsync((_automation, "automation"));

        Assert.Equal(1m, Assert.Single(result).RecommendationScore);
    }

    /// <summary>
    /// UTCID07 · B2=T · Type B — 3 tag đều khớp, kiểm tra làm tròn:
    /// (0.5000 + 0.2500 + 0.1667) / 3 = 0.9167 / 3 = 0.305566… ⇒ 0.3056.
    /// </summary>
    [Fact]
    public async Task UTCID07_ThreeMatchedTags_RoundsToFourDecimals()
    {
        var result = await RecommendAsync((_ai, "ai"), (_prompt, "prompt"), (_office, "office"));

        Assert.Equal(0.3056m, Assert.Single(result).RecommendationScore);
    }

    /// <summary>
    /// UTCID08 · B3=T · Type B — khóa học 4 tag, 2 khớp 2 lạc đề:
    /// (0.5000 + 0.2500) / 4 = 0.1875 — chứng minh hiệu ứng pha loãng khi gắn tag tràn lan.
    /// </summary>
    [Fact]
    public async Task UTCID08_ManyIrrelevantTags_DilutesRelevanceScore()
    {
        var result = await RecommendAsync(
            (_ai, "ai"),
            (_prompt, "prompt"),
            (Guid.NewGuid(), "python"),
            (Guid.NewGuid(), "machine-learning"));

        Assert.Equal(0.1875m, Assert.Single(result).RecommendationScore);
    }

    /// <summary>
    /// UTCID09 · B2=T, B3=T · Type N — MatchedTags chỉ chứa tag khớp và giữ đúng thứ tự
    /// khai báo tag của khóa học, để màn hình gợi ý giải thích được "vì sao khóa này".
    /// </summary>
    [Fact]
    public async Task UTCID09_MatchedTags_KeepCourseTagOrderAndExcludeUnmatched()
    {
        var result = await RecommendAsync(
            (Guid.NewGuid(), "python"),
            (_office, "office"),
            (_ai, "ai"));

        var course = Assert.Single(result);
        Assert.Equal(new[] { "office", "ai" }, course.MatchedTags);
        Assert.Equal(0.2222m, course.RecommendationScore); // (0 + 0.1667 + 0.5000) / 3
    }
}
