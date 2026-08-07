using AILA.Application.Tests.UnitTests.TestHelpers;
using AILA.Domain.Entities;
using AILA.Domain.Enums;

namespace AILA.Application.Tests.UnitTests;

/// <summary>
/// Sheet: UT21_RestorePublication — <see cref="Course.RestorePublication"/>
/// Module: Course · CC = 3 · 4 test case
///
/// Nhánh: B1 = !IsPublicationLocked (throw) · B2 = IsPublished (return im lặng)
///
/// Ghi chú white-box: tổ hợp "đang bị khóa VÀ đang published" KHÔNG đạt được qua API domain
/// (LockVisibility luôn đặt IsPublished = false), nên UTCID03 phải dựng bằng reflection.
/// </summary>
public class UT21_Course_RestorePublicationTests
{
    private static Course BuildCourse() =>
        new("Prompt Engineering 101", Guid.NewGuid(), Guid.NewGuid(), KnowledgeLevel.Beginner);

    private static Course BuildLockedCourse()
    {
        var course = BuildCourse();
        course.LockVisibility();
        return course;
    }

    /// <summary>UTCID01 · B1=T · Type A — khóa học không bị khóa.</summary>
    [Fact]
    public void UTCID01_NotLocked_ThrowsInvalidOperation()
    {
        var course = BuildCourse();

        var ex = Assert.Throws<InvalidOperationException>(() => course.RestorePublication());

        Assert.Equal("Khóa học không bị khóa.", ex.Message);
    }

    /// <summary>UTCID02 · B1=F, B2=F · Type N — gỡ khóa và publish lại.</summary>
    [Fact]
    public void UTCID02_LockedAndUnpublished_UnlocksAndPublishes()
    {
        var course = BuildLockedCourse();

        course.RestorePublication();

        Assert.False(course.IsPublicationLocked);
        Assert.True(course.IsPublished);
    }

    /// <summary>
    /// UTCID03 · B2=T · Type A — trạng thái bất thường: đang bị khóa nhưng vẫn published.
    /// Kỳ vọng: return im lặng, KHÔNG gỡ cờ khóa.
    /// </summary>
    [Fact]
    public void UTCID03_LockedButStillPublished_ReturnsSilentlyKeepingLock()
    {
        var course = BuildLockedCourse();
        PrivateSetter.Set(course, nameof(Course.IsPublished), true);

        course.RestorePublication();

        Assert.True(course.IsPublicationLocked);
        Assert.True(course.IsPublished);
    }

    /// <summary>UTCID04 · B1=T · Type B — gọi lần thứ hai sau khi đã gỡ khóa thành công.</summary>
    [Fact]
    public void UTCID04_CalledTwice_SecondCallThrows()
    {
        var course = BuildLockedCourse();
        course.RestorePublication();

        var ex = Assert.Throws<InvalidOperationException>(() => course.RestorePublication());

        Assert.Equal("Khóa học không bị khóa.", ex.Message);
    }
}
