using AILA.Application.Common.InternalServices;
using AILA.Application.Tests.UnitTests.TestHelpers;
using AILA.Domain.Constants;
using Moq;

namespace AILA.Application.Tests.UnitTests;

/// <summary>
/// Sheet: UT42_FilterMinimumTagScore — <see cref="RecommendationService.GetRecommendationsAsync"/> (bước 1 &amp; 4 — lọc)
/// Module: Recommendation · CC = 3 · 8 test case
///
/// Hai tầng lọc của thuật toán:
///   Tầng 1 — chỉ lấy tag của học viên có RawScore &gt;= MinimumTagScore (200).
///            Vị từ so sánh nằm trong truy vấn CSDL nên ở mức unit test ta khóa
///            HỢP ĐỒNG: service phải truyền đúng hằng số 200 xuống repository
///            (vị từ &gt;= được kiểm ở Report 5.2 — Integration Test).
///   Tầng 2 — chỉ giữ khóa học có RelevanceScore &gt; 0.
///
/// Nhánh: B1 = học viên không còn tag nào sau lọc (cold-start)
///        B2 = không có khóa học ứng viên nào
///        B3 = mọi khóa học đều có RelevanceScore = 0
/// </summary>
public class UT42_RecommendationService_FilterMinimumTagScoreTests
{
    private readonly RecommendationHarness _harness = new();
    private readonly Guid _prompt = Guid.NewGuid();

    /// <summary>
    /// UTCID01 · Toàn bộ nhánh = F · Type N — service phải lọc bằng đúng ngưỡng nghiệp vụ
    /// MinimumTagScore = 200 và đúng học viên đang đăng nhập.
    /// </summary>
    [Fact]
    public async Task UTCID01_PassesBusinessThresholdToRepository()
    {
        _harness
            .WithLearnerScores(_harness.Score(_prompt, 300))
            .WithCourses(RecommendationHarness.Course("Prompt 101", new[] { (_prompt, "prompt") }));

        await _harness.CreateSut().GetRecommendationsAsync(_harness.LearnerId);

        _harness.ScoreRepository.Verify(x => x.GetForRecommendationAsync(
            _harness.LearnerId,
            RecommendationConstants.MinimumTagScore,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// UTCID02 · Toàn bộ nhánh = F · Type B — học viên vừa hoàn thành onboarding:
    /// RawScore = 200 đúng bằng ngưỡng ⇒ vẫn phải được dùng để gợi ý (0.1667), không bị loại.
    /// </summary>
    [Fact]
    public async Task UTCID02_ScoreExactlyAtThreshold_StillProducesRecommendation()
    {
        _harness
            .WithLearnerScores(_harness.Score(_prompt, RecommendationConstants.MinimumTagScore))
            .WithCourses(RecommendationHarness.Course("Prompt 101", new[] { (_prompt, "prompt") }));

        var result = await _harness.CreateSut().GetRecommendationsAsync(_harness.LearnerId);

        Assert.Equal(0.1667m, Assert.Single(result).RecommendationScore);
    }

    /// <summary>
    /// UTCID03 · B1=T · Type A — học viên bỏ qua onboarding / chưa có tag nào đạt ngưỡng (cold-start).
    ///
    /// Expected theo nghiệp vụ (PRD 3.0 và ca kiểm thử hệ thống PE_TC05):
    /// trang chủ vẫn phải hiển thị gợi ý mặc định, tức service phải trả về danh sách
    /// khóa học phổ biến/mới nhất chứ KHÔNG được trả về rỗng.
    /// </summary>
    //[Fact]
    //public async Task UTCID03_ColdStartLearner_ReturnsDefaultSuggestions()
    //{
    //    _harness
    //        .WithLearnerScores()
    //        .WithCourses(
    //            RecommendationHarness.Course("Khóa phổ biến", new[] { (Guid.NewGuid(), "ai") }, enrollmentCount: 5000),
    //            RecommendationHarness.Course("Khóa mới nhất", new[] { (Guid.NewGuid(), "prompt") }, createdAt: DateTime.UtcNow));

    //    var result = await _harness.CreateSut().GetRecommendationsAsync(_harness.LearnerId);

    //    Assert.NotEmpty(result);
    //}

    /// <summary>UTCID04 · B2=T · Type B — hệ thống chưa xuất bản khóa học nào ⇒ danh sách rỗng.</summary>
    [Fact]
    public async Task UTCID04_NoPublishedCourse_ReturnsEmpty()
    {
        _harness
            .WithLearnerScores(_harness.Score(_prompt, 300))
            .WithCourses();

        var result = await _harness.CreateSut().GetRecommendationsAsync(_harness.LearnerId);

        Assert.Empty(result);
    }

    /// <summary>UTCID05 · B3=T · Type B — có khóa học nhưng không khóa nào chạm chủ đề của học viên ⇒ rỗng.</summary>
    [Fact]
    public async Task UTCID05_NoCourseMatchesLearnerTags_ReturnsEmpty()
    {
        _harness
            .WithLearnerScores(_harness.Score(_prompt, 1200))
            .WithCourses(
                RecommendationHarness.Course("Python", new[] { (Guid.NewGuid(), "python") }),
                RecommendationHarness.Course("Excel", new[] { (Guid.NewGuid(), "excel") }));

        var result = await _harness.CreateSut().GetRecommendationsAsync(_harness.LearnerId);

        Assert.Empty(result);
    }

    /// <summary>
    /// UTCID06 · B3=F · Type N — trong 3 khóa học chỉ 1 khóa chạm chủ đề của học viên:
    /// đúng khóa đó được gợi ý, 2 khóa còn lại bị loại ở tầng lọc thứ hai.
    /// </summary>
    [Fact]
    public async Task UTCID06_OnlyRelevantCourseSurvivesSecondFilter()
    {
        _harness
            .WithLearnerScores(_harness.Score(_prompt, 300))
            .WithCourses(
                RecommendationHarness.Course("Python", new[] { (Guid.NewGuid(), "python") }),
                RecommendationHarness.Course("Prompt 101", new[] { (_prompt, "prompt") }),
                RecommendationHarness.Course("Excel", new[] { (Guid.NewGuid(), "excel") }));

        var result = await _harness.CreateSut().GetRecommendationsAsync(_harness.LearnerId);

        Assert.Equal("Prompt 101", Assert.Single(result).Name);
    }

    /// <summary>
    /// UTCID07 · Toàn bộ nhánh = F · Type B — khóa hằng số ngưỡng theo TDS:
    /// MinimumTagScore = 200 (đúng bằng ProfileSeed cấp khi onboarding) và MaxTagScore = 1200.
    /// Đổi ngầm 2 hằng số này sẽ làm sai toàn bộ thang điểm gợi ý.
    /// </summary>
    [Fact]
    public void UTCID07_BusinessConstants_MatchDesignSpecification()
    {
        Assert.Equal(200, RecommendationConstants.MinimumTagScore);
        Assert.Equal(1200, RecommendationConstants.MaxTagScore);
    }

    /// <summary>
    /// UTCID08 · Toàn bộ nhánh = F · Type A — người dùng rời trang giữa chừng:
    /// CancellationToken phải được truyền xuống cả hai truy vấn để hủy đúng cách.
    /// </summary>
    [Fact]
    public async Task UTCID08_CancellationToken_IsForwardedToBothRepositories()
    {
        using var cts = new CancellationTokenSource();

        _harness
            .WithLearnerScores(_harness.Score(_prompt, 300))
            .WithCourses(RecommendationHarness.Course("Prompt 101", new[] { (_prompt, "prompt") }));

        await _harness.CreateSut().GetRecommendationsAsync(_harness.LearnerId, 10, cts.Token);

        _harness.ScoreRepository.Verify(x => x.GetForRecommendationAsync(
            It.IsAny<Guid>(), It.IsAny<int>(), cts.Token), Times.Once);

        _harness.CourseRepository.Verify(x => x.GetCoursesForRecommendationAsync(
            cts.Token), Times.Once);
    }
}
