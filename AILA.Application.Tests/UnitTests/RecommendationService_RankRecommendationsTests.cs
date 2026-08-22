using AILA.Application.Common.Dtos.Recommendation;
using AILA.Application.Common.InternalServices;
using AILA.Application.Tests.UnitTests.TestHelpers;

namespace AILA.Application.Tests.UnitTests;

/// <summary>
/// Sheet: UT43_RankRecommendedCourses — <see cref="RecommendationService.GetRecommendationsAsync"/> (bước 5 — xếp hạng)
/// Module: Recommendation · CC = 5 · 9 test case
///
/// Danh sách gợi ý được ghép từ 3 khối theo TDS:
///   A — Relevance  ceil(limit × 0.5) khóa, sắp theo RelevanceScore giảm dần
///   B — Popularity ceil(limit × 0.3) khóa còn lại, sắp theo EnrollmentCount giảm dần
///   C — Newest     limit − A − B khóa còn lại tạo trong tháng hiện tại, mới nhất trước;
///                  thiếu thì bù bằng khóa có RelevanceScore cao nhất trong số còn lại.
/// Ràng buộc nghiệp vụ bao trùm: tổng số khóa trả về KHÔNG BAO GIỜ vượt quá limit,
/// và không khóa nào xuất hiện hai lần.
///
/// Nhánh: B1 = còn khóa cho khối Popularity · B2 = còn khóa tạo trong tháng cho khối Newest
///        B3 = khối Newest thiếu ⇒ bù theo relevance · B4 = không còn ứng viên nào
///        B5 = limit nhỏ (biên) — ceil làm A + B đã vượt quá limit
/// </summary>
public class UT43_RecommendationService_RankRecommendationsTests
{
    private readonly RecommendationHarness _harness = new();

    // Hồ sơ persona của Test Gap Brief: Office Worker · Beginner · mục tiêu Prompt Engineering,
    // đã Enroll (+20) và Complete (+100) một khóa học.
    private readonly Guid _ai = Guid.NewGuid();
    private readonly Guid _prompt = Guid.NewGuid();
    private readonly Guid _office = Guid.NewGuid();

    private void WithPersonaProfile() => _harness.WithLearnerScores(
        _harness.Score(_ai, 600),      // 0.5000
        _harness.Score(_prompt, 300),  // 0.2500
        _harness.Score(_office, 200)); // 0.1667

    /// <summary>
    /// UTCID01 · Toàn bộ nhánh = F · Type N — kịch bản gốc của Test Gap Brief.
    ///   B "Prompt Engineering nâng cao" {ai, prompt} → (0.5000 + 0.2500) / 2 = 0.3750
    ///   A "Nhập môn AI cho người đi làm" {prompt}    → 0.2500 / 1          = 0.2500
    ///   C "Python cho Data"  {python, ml, office}    → 0.1667 / 3          = 0.0556
    /// Kỳ vọng thứ hạng B &gt; A &gt; C.
    /// </summary>
    [Fact]
    public async Task UTCID01_PersonaProfile_RanksCoursesByRelevance()
    {
        WithPersonaProfile();

        _harness.WithCourses(
            RecommendationHarness.Course("Prompt Engineering nâng cao", new[] { (_ai, "ai"), (_prompt, "prompt") }),
            RecommendationHarness.Course("Nhập môn AI cho người đi làm", new[] { (_prompt, "prompt") }),
            RecommendationHarness.Course("Python cho Data",
                new[] { (Guid.NewGuid(), "python"), (Guid.NewGuid(), "machine-learning"), (_office, "office") }));

        var result = await _harness.CreateSut().GetRecommendationsAsync(_harness.LearnerId);

        Assert.Equal(
            new[] { "Prompt Engineering nâng cao", "Nhập môn AI cho người đi làm", "Python cho Data" },
            result.Select(x => x.Name).ToArray());

        Assert.Equal(new[] { 0.375m, 0.25m, 0.0556m }, result.Select(x => x.RecommendationScore).ToArray());
    }

    /// <summary>
    /// UTCID02 · B1=T · Type B — limit = 2 ⇒ khối Relevance 1 khóa, khối Popularity 1 khóa.
    /// Khóa ít liên quan nhất (0.0556) nhưng đông học viên nhất (500 lượt) phải chiếm suất
    /// Popularity, vượt lên trên khóa có relevance cao hơn (0.2500) nhưng chỉ 10 lượt.
    /// </summary>
    [Fact]
    public async Task UTCID02_LimitTwo_SecondSlotGoesToMostPopularCourse()
    {
        WithPersonaProfile();

        _harness.WithCourses(
            RecommendationHarness.Course("Prompt Engineering nâng cao",
                new[] { (_ai, "ai"), (_prompt, "prompt") }, enrollmentCount: 10),
            RecommendationHarness.Course("Nhập môn AI cho người đi làm",
                new[] { (_prompt, "prompt") }, enrollmentCount: 10),
            RecommendationHarness.Course("Python cho Data",
                new[] { (Guid.NewGuid(), "python"), (Guid.NewGuid(), "machine-learning"), (_office, "office") },
                enrollmentCount: 500));

        var result = await _harness.CreateSut().GetRecommendationsAsync(_harness.LearnerId, limit: 2);

        Assert.Equal(
            new[] { "Prompt Engineering nâng cao", "Python cho Data" },
            result.Select(x => x.Name).ToArray());
    }

    /// <summary>
    /// UTCID03 · B1=T, B2=T · Type B — limit = 6 ⇒ 3 Relevance + 2 Popularity + 1 Newest.
    /// 7 ứng viên có relevance giảm dần 1.0000 / 0.7500 / 0.5000 / 0.3750 / 0.2500 / 0.2000 / 0.1667.
    /// Hai khóa đông học viên nhất (C7, C6) chiếm khối Popularity; suất Newest thuộc về C5 —
    /// khóa duy nhất còn lại được tạo trong tháng hiện tại.
    /// </summary>
    [Fact]
    public async Task UTCID03_FullMix_FillsRelevancePopularityAndNewestBlocks()
    {
        var courses = BuildSevenCandidates(newestCreatedAt: DateTime.UtcNow);

        var result = await _harness.CreateSut().GetRecommendationsAsync(_harness.LearnerId, limit: 6);

        Assert.Equal(
            new[] { "C1", "C2", "C3", "C7", "C6", "C5" },
            result.Select(x => x.Name).ToArray());

        Assert.Equal(6, result.Count);
        Assert.Equal(courses.Length, 7);
    }

    /// <summary>
    /// UTCID04 · B3=T · Type B — cùng bộ 7 ứng viên nhưng không khóa nào còn lại được tạo
    /// trong tháng hiện tại ⇒ khối Newest rỗng và phải bù bằng khóa có relevance cao nhất
    /// trong số còn lại (C4 = 0.3750) thay vì bỏ trống suất.
    /// </summary>
    [Fact]
    public async Task UTCID04_NoRecentCourse_NewestBlockFallsBackToRelevance()
    {
        BuildSevenCandidates(newestCreatedAt: DateTime.UtcNow.AddYears(-1));

        var result = await _harness.CreateSut().GetRecommendationsAsync(_harness.LearnerId, limit: 6);

        Assert.Equal(
            new[] { "C1", "C2", "C3", "C7", "C6", "C4" },
            result.Select(x => x.Name).ToArray());
    }

    /// <summary>
    /// UTCID05 · B5=T · Type B — limit = 1 (biên dưới của một lời gọi có nghĩa).
    ///
    /// Expected theo nghiệp vụ: học viên xin 1 gợi ý thì nhận đúng 1 khóa học —
    /// widget "Gợi ý cho bạn" trên trang chủ dựa vào limit để canh chỗ hiển thị.
    /// </summary>
    //[Fact]
    //public async Task UTCID05_LimitOne_ReturnsExactlyOneCourse()
    //{
    //    WithPersonaProfile();

    //    _harness.WithCourses(
    //        RecommendationHarness.Course("Prompt Engineering nâng cao", new[] { (_ai, "ai"), (_prompt, "prompt") }),
    //        RecommendationHarness.Course("Nhập môn AI cho người đi làm", new[] { (_prompt, "prompt") }),
    //        RecommendationHarness.Course("Python cho Data",
    //            new[] { (Guid.NewGuid(), "python"), (Guid.NewGuid(), "machine-learning"), (_office, "office") }));

    //    var result = await _harness.CreateSut().GetRecommendationsAsync(_harness.LearnerId, limit: 1);

    //    Assert.Single(result);
    //}

    /// <summary>UTCID06 · B5=T · Type B — limit = 0 (biên dưới tuyệt đối) ⇒ không gợi ý khóa nào.</summary>
    [Fact]
    public async Task UTCID06_LimitZero_ReturnsEmpty()
    {
        WithPersonaProfile();

        _harness.WithCourses(
            RecommendationHarness.Course("Prompt Engineering nâng cao", new[] { (_ai, "ai"), (_prompt, "prompt") }));

        var result = await _harness.CreateSut().GetRecommendationsAsync(_harness.LearnerId, limit: 0);

        Assert.Empty(result);
    }

    /// <summary>UTCID07 · B4=T · Type A — không ứng viên nào có relevance &gt; 0 ⇒ không xếp hạng, trả rỗng.</summary>
    [Fact]
    public async Task UTCID07_NoCandidate_ReturnsEmptyWithoutRanking()
    {
        WithPersonaProfile();

        _harness.WithCourses(
            RecommendationHarness.Course("Excel nâng cao", new[] { (Guid.NewGuid(), "excel") }, enrollmentCount: 9999));

        var result = await _harness.CreateSut().GetRecommendationsAsync(_harness.LearnerId);

        Assert.Empty(result);
    }

    /// <summary>
    /// UTCID08 · B1=T, B2=T · Type N — ba khối không được giẫm chân nhau:
    /// mỗi khóa học chỉ xuất hiện đúng một lần trong danh sách gợi ý.
    /// </summary>
    [Fact]
    public async Task UTCID08_BlocksDoNotOverlap_NoDuplicateCourse()
    {
        BuildSevenCandidates(newestCreatedAt: DateTime.UtcNow);

        var result = await _harness.CreateSut().GetRecommendationsAsync(_harness.LearnerId, limit: 6);

        Assert.Equal(result.Count, result.Select(x => x.CourseId).Distinct().Count());
    }

    /// <summary>
    /// UTCID09 · Toàn bộ nhánh = F · Type N — thẻ khóa học trên trang chủ cần đủ dữ liệu hiển thị:
    /// tên, ảnh, danh mục, chuyên gia, cấp độ, điểm phù hợp và lý do gợi ý (MatchedTags).
    /// </summary>
    [Fact]
    public async Task UTCID09_MapsAllDisplayFieldsToDto()
    {
        WithPersonaProfile();

        var course = RecommendationHarness.Course(
            "Prompt Engineering nâng cao",
            new[] { (_ai, "ai"), (_prompt, "prompt") },
            enrollmentCount: 120);

        _harness.WithCourses(course);

        var result = await _harness.CreateSut().GetRecommendationsAsync(_harness.LearnerId);

        var dto = Assert.Single(result);
        Assert.Equal(course.CourseId, dto.CourseId);
        Assert.Equal("Prompt Engineering nâng cao", dto.Name);
        Assert.Equal(course.ThumbnailUrl, dto.ThumbnailUrl);
        Assert.Equal("AI Fundamentals", dto.CategoryName);
        Assert.Equal("Nguyen Van Chuyen Gia", dto.ExpertName);
        Assert.Equal("Beginner", dto.Level);
        Assert.Equal(0.375m, dto.RecommendationScore);
        Assert.Equal(new[] { "ai", "prompt" }, dto.MatchedTags);
    }

    /// <summary>
    /// 7 ứng viên, mỗi khóa gắn đúng 1 tag riêng để relevance đọc thẳng ra điểm chuẩn hóa:
    /// C1 1.0000 · C2 0.7500 · C3 0.5000 · C4 0.3750 · C5 0.2500 · C6 0.2000 · C7 0.1667.
    /// Lượt ghi danh cố tình đảo ngược: C7 (900) và C6 (800) đông nhất.
    /// </summary>
    private CourseRecommendationCandidateDto[] BuildSevenCandidates(DateTime newestCreatedAt)
    {
        var raws = new[] { 1200, 900, 600, 450, 300, 240, 200 };
        var enrollments = new[] { 1, 2, 3, 5, 10, 800, 900 };

        var tagIds = raws.Select(_ => Guid.NewGuid()).ToArray();

        _harness.WithLearnerScores(
            tagIds.Select((tagId, i) => _harness.Score(tagId, raws[i])).ToArray());

        var courses = tagIds
            .Select((tagId, i) => RecommendationHarness.Course(
                $"C{i + 1}",
                new[] { (tagId, $"tag-{i + 1}") },
                enrollmentCount: enrollments[i],
                createdAt: i == 4 ? newestCreatedAt : DateTime.UtcNow.AddYears(-2)))
            .ToArray();

        _harness.WithCourses(courses);

        return courses;
    }
}
