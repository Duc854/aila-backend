using AILA.Application.Common.InternalServices;
using AILA.Application.Tests.UnitTests.TestHelpers;
using AILA.Domain.Constants;

namespace AILA.Application.Tests.UnitTests;

/// <summary>
/// Sheet: UT40_NormalizeTagScore — <see cref="RecommendationService.GetRecommendationsAsync"/> (bước 3 — chuẩn hóa)
/// Module: Recommendation · CC = 3 · 9 test case
///
/// Công thức TDS: NormalizedScore = min(RawScore / MaxTagScore, 1), làm tròn 4 chữ số thập phân.
/// MaxTagScore = 1200.
///
/// Phạm vi: chỉ bước chuẩn hóa điểm tag của học viên. Mỗi khóa học trong test case
/// được gắn ĐÚNG 1 tag, khi đó RelevanceScore = tổng điểm khớp / 1 = chính giá trị
/// chuẩn hóa ⇒ đọc trực tiếp NormalizedScore qua RecommendationScore của DTO trả về.
/// Bước tính relevance nhiều tag nằm ở sheet UT41, bước xếp hạng nằm ở sheet UT43.
///
/// Nhánh: B1 = RawScore / MaxTagScore &gt; 1 (chạm trần chuẩn hóa)
///        B2 = tag của khóa học không nằm trong hồ sơ học viên (không chuẩn hóa)
///        B3 = điểm chuẩn hóa = 0 ⇒ khóa học bị loại khỏi kết quả
/// </summary>
public class UT40_RecommendationService_NormalizeTagScoreTests
{
    private readonly RecommendationHarness _harness = new();
    private readonly Guid _tagId = Guid.NewGuid();

    /// <summary>Dựng 1 học viên có đúng 1 tag điểm thô = rawScore và 1 khóa học gắn đúng tag đó.</summary>
    private async Task<decimal?> ScoreOfSingleTagCourseAsync(int rawScore)
    {
        _harness
            .WithLearnerScores(_harness.Score(_tagId, rawScore))
            .WithCourses(RecommendationHarness.Course(
                "Prompt Engineering 101",
                new[] { (_tagId, "prompt-engineering") }));

        var result = await _harness.CreateSut()
            .GetRecommendationsAsync(_harness.LearnerId);

        return result.Count == 0 ? null : result[0].RecommendationScore;
    }

    /// <summary>UTCID01 · Toàn bộ nhánh = F · Type N — RawScore 300 (seed 200 + complete 100) ⇒ 300/1200 = 0.2500.</summary>
    [Fact]
    public async Task UTCID01_RawScore300_NormalizesTo0_2500()
    {
        Assert.Equal(0.25m, await ScoreOfSingleTagCourseAsync(300));
    }

    /// <summary>
    /// UTCID02 · Toàn bộ nhánh = F · Type B — RawScore 200 = đúng ngưỡng MinimumTagScore
    /// (học viên vừa onboarding xong) ⇒ 200/1200 = 0.166666… làm tròn 4 số = 0.1667.
    /// </summary>
    [Fact]
    public async Task UTCID02_RawScoreAtMinimumThreshold_NormalizesTo0_1667()
    {
        Assert.Equal(200, RecommendationConstants.MinimumTagScore);
        Assert.Equal(0.1667m, await ScoreOfSingleTagCourseAsync(200));
    }

    /// <summary>
    /// UTCID03 · B1=F (biên) · Type B — RawScore 1200 = MaxTagScore (seed 200 + hành vi kịch trần 1000)
    /// ⇒ 1200/1200 = 1.0000, đúng trần chuẩn hóa.
    /// </summary>
    [Fact]
    public async Task UTCID03_RawScoreAtMaxTagScore_NormalizesToOne()
    {
        Assert.Equal(1200, RecommendationConstants.MaxTagScore);
        Assert.Equal(1m, await ScoreOfSingleTagCourseAsync(1200));
    }

    /// <summary>UTCID04 · Toàn bộ nhánh = F · Type N — RawScore 600 ⇒ 0.5000 (đúng nửa thang điểm).</summary>
    [Fact]
    public async Task UTCID04_RawScore600_NormalizesTo0_5000()
    {
        Assert.Equal(0.5m, await ScoreOfSingleTagCourseAsync(600));
    }

    /// <summary>UTCID05 · Toàn bộ nhánh = F · Type B — RawScore 100 ⇒ 0.083333… làm tròn xuống = 0.0833.</summary>
    [Fact]
    public async Task UTCID05_RawScore100_RoundsDownTo0_0833()
    {
        Assert.Equal(0.0833m, await ScoreOfSingleTagCourseAsync(100));
    }

    /// <summary>UTCID06 · Toàn bộ nhánh = F · Type B — RawScore 400 ⇒ 0.333333… làm tròn = 0.3333.</summary>
    [Fact]
    public async Task UTCID06_RawScore400_RoundsTo0_3333()
    {
        Assert.Equal(0.3333m, await ScoreOfSingleTagCourseAsync(400));
    }

    /// <summary>
    /// UTCID07 · B3=T · Type B — RawScore 0 (biên dưới) ⇒ điểm chuẩn hóa 0
    /// ⇒ khóa học không đủ liên quan, không được đưa vào danh sách gợi ý.
    /// </summary>
    [Fact]
    public async Task UTCID07_RawScoreZero_CourseIsExcluded()
    {
        Assert.Null(await ScoreOfSingleTagCourseAsync(0));
    }

    /// <summary>
    /// UTCID08 · Toàn bộ nhánh = F · Type N — học viên có nhiều tag, khóa học chỉ gắn 1 tag:
    /// chỉ tag khớp được dùng để tính điểm, MatchedTags nêu đúng tag đó.
    /// </summary>
    [Fact]
    public async Task UTCID08_MultipleLearnerTags_UsesOnlyMatchedTag()
    {
        var otherTagId = Guid.NewGuid();

        _harness
            .WithLearnerScores(
                _harness.Score(_tagId, 300),        // 0.2500 — khớp
                _harness.Score(otherTagId, 1200))   // 1.0000 — không khớp, phải bị bỏ qua
            .WithCourses(RecommendationHarness.Course(
                "Prompt Engineering 101",
                new[] { (_tagId, "prompt-engineering") }));

        var result = await _harness.CreateSut()
            .GetRecommendationsAsync(_harness.LearnerId);

        var course = Assert.Single(result);
        Assert.Equal(0.25m, course.RecommendationScore);
        Assert.Equal(new[] { "prompt-engineering" }, course.MatchedTags);
    }

    /// <summary>
    /// UTCID09 · B2=T · Type A — khóa học gắn tag lạ (không có trong hồ sơ học viên):
    /// không có giá trị chuẩn hóa nào để cộng ⇒ điểm 0 ⇒ bị loại khỏi danh sách gợi ý.
    /// </summary>
    [Fact]
    public async Task UTCID09_CourseTagNotInLearnerProfile_CourseIsExcluded()
    {
        _harness
            .WithLearnerScores(_harness.Score(_tagId, 1200))
            .WithCourses(RecommendationHarness.Course(
                "Machine Learning cơ bản",
                new[] { (Guid.NewGuid(), "machine-learning") }));

        var result = await _harness.CreateSut()
            .GetRecommendationsAsync(_harness.LearnerId);

        Assert.Empty(result);
    }
}
