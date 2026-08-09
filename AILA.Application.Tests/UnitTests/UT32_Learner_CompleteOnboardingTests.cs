using AILA.Domain.Entities;
using AILA.Domain.Enums;

namespace AILA.Application.Tests.UnitTests;

/// <summary>
/// Sheet: UT32_CompleteOnboarding — <see cref="Learner.CompleteOnboarding"/>
/// Module: Onboarding · CC = 6 · 7 test case
///
/// Nhánh: B1 = HasCompletedOnboarding (throw) · B2 = tags null · B3 = tags rỗng
///        B4 = có tag chưa được duyệt (IsPublished = false) · B5 = danh sách có phần tử trùng
///
/// B4 là hàng rào toàn vẹn dữ liệu: học viên chỉ được chọn mục tiêu đã được Admin phê duyệt,
/// chặn Custom Tag do Expert tự tạo lọt vào hồ sơ học viên.
/// </summary>
public class UT32_Learner_CompleteOnboardingTests
{
    private static Learner BuildLearner() => new(Guid.NewGuid());

    private static Tag PublishedTag(string name) => Tag.CreateByAdmin(name, name);

    private static Tag UnpublishedTag(string name) => Tag.CreateByExpert(name, name, Guid.NewGuid());

    /// <summary>UTCID01 · Toàn bộ nhánh = F · Type N — hoàn thành khảo sát với 2 mục tiêu hợp lệ.</summary>
    [Fact]
    public void UTCID01_ValidTags_CompletesOnboarding()
    {
        var learner = BuildLearner();
        var tags = new List<Tag> { PublishedTag("prompt-basic"), PublishedTag("chatgpt") };

        learner.CompleteOnboarding(LearnerType.Student, KnowledgeLevel.Beginner, tags);

        Assert.True(learner.HasCompletedOnboarding);
        Assert.Equal(LearnerType.Student, learner.LearnerType);
        Assert.Equal(KnowledgeLevel.Beginner, learner.KnowledgeLevel);
        Assert.Equal(2, learner.LearningGoals.Count);
        Assert.NotNull(learner.UpdatedAt);
    }

    /// <summary>UTCID02 · B1=T · Type A — đã hoàn thành khảo sát trước đó.</summary>
    [Fact]
    public void UTCID02_AlreadyCompleted_ThrowsInvalidOperation()
    {
        var learner = BuildLearner();
        learner.CompleteOnboarding(
            LearnerType.Student, KnowledgeLevel.Beginner, new List<Tag> { PublishedTag("prompt-basic") });

        var ex = Assert.Throws<InvalidOperationException>(() =>
            learner.CompleteOnboarding(
                LearnerType.Freelancer, KnowledgeLevel.Advanced, new List<Tag> { PublishedTag("chatgpt") }));

        Assert.Equal("Học viên đã hoàn thành khảo sát ban đầu.", ex.Message);
        Assert.Equal(LearnerType.Student, learner.LearnerType);
    }

    /// <summary>UTCID03 · B2=T · Type A — danh sách mục tiêu null.</summary>
    [Fact]
    public void UTCID03_NullTags_ThrowsArgumentException()
    {
        var learner = BuildLearner();

        var ex = Assert.Throws<ArgumentException>(() =>
            learner.CompleteOnboarding(LearnerType.Student, KnowledgeLevel.Beginner, null!));

        Assert.Contains("Học viên phải chọn ít nhất một mục tiêu học tập.", ex.Message);
        Assert.False(learner.HasCompletedOnboarding);
    }

    /// <summary>UTCID04 · B2=F, B3=T · Type B — danh sách rỗng (biên dưới).</summary>
    [Fact]
    public void UTCID04_EmptyTags_ThrowsArgumentException()
    {
        var learner = BuildLearner();

        Assert.Throws<ArgumentException>(() =>
            learner.CompleteOnboarding(LearnerType.Student, KnowledgeLevel.Beginner, new List<Tag>()));
        Assert.False(learner.HasCompletedOnboarding);
    }

    /// <summary>UTCID05 · B4=T · Type A — chứa Tag chưa được Admin phê duyệt.</summary>
    [Fact]
    public void UTCID05_ContainsUnpublishedTag_ThrowsInvalidOperation()
    {
        var learner = BuildLearner();
        var tags = new List<Tag> { PublishedTag("prompt-basic"), UnpublishedTag("tag-cua-expert") };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            learner.CompleteOnboarding(LearnerType.Student, KnowledgeLevel.Beginner, tags));

        Assert.Equal("Không thể chọn mục tiêu học tập chưa được phê duyệt.", ex.Message);
        Assert.False(learner.HasCompletedOnboarding);
        Assert.Empty(learner.LearningGoals);
    }

    /// <summary>UTCID06 · B5=T · Type A — danh sách chứa mục tiêu trùng lặp.</summary>
    [Fact]
    public void UTCID06_DuplicatedTags_ThrowsInvalidOperation()
    {
        var learner = BuildLearner();
        var tag = PublishedTag("prompt-basic");
        var tags = new List<Tag> { tag, tag };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            learner.CompleteOnboarding(LearnerType.Student, KnowledgeLevel.Beginner, tags));

        Assert.Equal("Danh sách mục tiêu học tập không được chứa mục tiêu trùng lặp.", ex.Message);
        Assert.False(learner.HasCompletedOnboarding);
    }

    /// <summary>UTCID07 · B3=F · Type B — đúng 1 mục tiêu (biên dưới hợp lệ).</summary>
    [Fact]
    public void UTCID07_ExactlyOneTag_CompletesOnboarding()
    {
        var learner = BuildLearner();

        learner.CompleteOnboarding(
            LearnerType.OfficeWorker, KnowledgeLevel.Intermediate, new List<Tag> { PublishedTag("chatgpt") });

        Assert.True(learner.HasCompletedOnboarding);
        Assert.Single(learner.LearningGoals);
    }
}
