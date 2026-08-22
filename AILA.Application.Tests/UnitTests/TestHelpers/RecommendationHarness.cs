using AILA.Application.Common.Dtos.Recommendation;
using AILA.Application.Common.InternalServices;
using AILA.Application.Common.Interfaces;
using AILA.Application.Common.Interfaces.Repositories;
using AILA.Domain.Constants;
using AILA.Domain.Entities;
using Moq;

namespace AILA.Application.Tests.UnitTests.TestHelpers;

/// <summary>
/// Bộ khung dựng dữ liệu cho các sheet UT40–UT43 (Personalized Course Recommendation).
///
/// RecommendationService chỉ phụ thuộc IUnitOfWork nên toàn bộ input của thuật toán
/// đi qua đúng 2 cửa: LearnerTagScores.GetForRecommendationAsync và
/// Courses.GetCoursesForRecommendationAsync. Harness mock cả hai để mỗi test case
/// nạp được đúng bộ số cần thiết và tính tay được Expected Output.
/// </summary>
internal sealed class RecommendationHarness
{
    public Mock<IUnitOfWork> UnitOfWork { get; } = new();

    public Mock<ILearnerTagScoreRepository> ScoreRepository { get; } = new();

    public Mock<ICourseRepository> CourseRepository { get; } = new();

    public Guid LearnerId { get; } = Guid.NewGuid();

    public RecommendationHarness()
    {
        UnitOfWork.SetupGet(x => x.LearnerTagScores).Returns(ScoreRepository.Object);
        UnitOfWork.SetupGet(x => x.Courses).Returns(CourseRepository.Object);

        WithLearnerScores();
        WithCourses();
    }

    /// <summary>Danh sách LearnerTagScore đã vượt ngưỡng MinimumTagScore (kết quả của bước lọc).</summary>
    public RecommendationHarness WithLearnerScores(params LearnerTagScore[] scores)
    {
        ScoreRepository
            .Setup(x => x.GetForRecommendationAsync(
                It.IsAny<Guid>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(scores.ToList());

        return this;
    }

    public RecommendationHarness WithCourses(params CourseRecommendationCandidateDto[] courses)
    {
        CourseRepository
            .Setup(x => x.GetCoursesForRecommendationAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(courses.ToList());

        return this;
    }

    public RecommendationService CreateSut() => new(UnitOfWork.Object);

    /// <summary>
    /// Dựng LearnerTagScore có RawScore = <paramref name="rawScore"/>.
    /// ProfileSeed lấy tối đa 200 (trần nghiệp vụ), phần dư đẩy vào BehaviorScore
    /// đúng như luồng thật: onboarding seed 200 rồi hành vi cộng dồn.
    /// </summary>
    public LearnerTagScore Score(Guid tagId, int rawScore)
    {
        var profileSeed = Math.Min(rawScore, 200);
        var score = new LearnerTagScore(LearnerId, tagId, profileSeed);

        var behavior = rawScore - profileSeed;
        if (behavior > 0)
            score.IncreaseBehaviorScore(behavior);

        return score;
    }

    public static CourseRecommendationCandidateDto Course(
        string name,
        (Guid Id, string Name)[] tags,
        int enrollmentCount = 0,
        DateTime? createdAt = null) => new()
        {
            CourseId = Guid.NewGuid(),
            Name = name,
            ThumbnailUrl = $"https://cdn.aila.vn/{name}.png",
            CategoryName = "AI Fundamentals",
            ExpertName = "Nguyen Van Chuyen Gia",
            Level = "Beginner",
            EnrollmentCount = enrollmentCount,
            CreatedAt = createdAt ?? DateTime.MinValue,
            Tags = tags
                .Select(t => new CourseTagCandidateDto { Id = t.Id, Name = t.Name })
                .ToList()
        };

    /// <summary>Điểm chuẩn hóa kỳ vọng — tính lại theo đúng công thức TDS để đối chiếu.</summary>
    public static decimal ExpectedNormalized(int rawScore) => Math.Min(
        Math.Round(rawScore / (decimal)RecommendationConstants.MaxTagScore, 4),
        1m);
}
