using AILA.Domain.Entities;
using AILA.Domain.Enums;

namespace AILA.Application.Tests.UnitTests;

/// <summary>
/// Sheet: UT19_CoursePublish — <see cref="Course.Publish"/>
/// Module: Course · CC = 5 · 6 test case
///
/// Nhánh: B1 = IsPublished (return im lặng) · B2 = IsPublicationLocked (throw)
///        B3 = không có Module nào (throw) · B4 = vòng lặp validate từng Module
///
/// Publish là state machine kèm validate lan truyền xuống Module: mỗi Module bắt buộc
/// phải có ít nhất một học liệu, nếu không sẽ ném từ chính Module.ValidateBeforeCoursePublish().
/// </summary>
public class UT19_Course_PublishTests
{
    private static Course BuildCourse() =>
        new("Prompt Engineering 101", Guid.NewGuid(), Guid.NewGuid(), KnowledgeLevel.Beginner);

    private static Module BuildModule(Guid courseId, int orderIndex, bool withMaterial)
    {
        var module = new Module(courseId, $"Học phần số {orderIndex}", orderIndex);
        if (withMaterial)
            module.AddMaterial(Material.CreateVideo(module.Id, $"Bài học {orderIndex}", 1));
        return module;
    }

    /// <summary>UTCID01 · B1=T · Type A — khóa học đã publish: gọi lại không đổi gì.</summary>
    [Fact]
    public void UTCID01_AlreadyPublished_ReturnsSilently()
    {
        var course = BuildCourse();
        course.AddModule(BuildModule(course.Id, 1, withMaterial: true));
        course.Publish();
        var updatedAt = course.UpdatedAt;

        course.Publish();

        Assert.True(course.IsPublished);
        Assert.Equal(updatedAt, course.UpdatedAt);
    }

    /// <summary>UTCID02 · B1=F, B2=T · Type A — khóa học bị khóa do bị tố cáo.</summary>
    [Fact]
    public void UTCID02_PublicationLocked_ThrowsInvalidOperation()
    {
        var course = BuildCourse();
        course.AddModule(BuildModule(course.Id, 1, withMaterial: true));
        course.LockVisibility();

        var ex = Assert.Throws<InvalidOperationException>(() => course.Publish());

        Assert.Equal(
            "Khóa học này cần được admin phê duyệt lại để publish do đã bị tố cáo",
            ex.Message);
        Assert.False(course.IsPublished);
    }

    /// <summary>UTCID03 · B3=T · Type B — không có học phần nào (tập rỗng).</summary>
    [Fact]
    public void UTCID03_WithoutAnyModule_ThrowsInvalidOperation()
    {
        var course = BuildCourse();

        var ex = Assert.Throws<InvalidOperationException>(() => course.Publish());

        Assert.Equal("Khóa học phải có ít nhất một học phần trước khi xuất bản.", ex.Message);
        Assert.False(course.IsPublished);
    }

    /// <summary>
    /// UTCID04 · B3=F, B4 (validate lan truyền) · Type A — học phần chưa có học liệu.
    /// Exception đến từ Module.ValidateBeforeCoursePublish(), không phải từ Course.
    /// </summary>
    [Fact]
    public void UTCID04_ModuleWithoutMaterial_ThrowsFromModuleValidation()
    {
        var course = BuildCourse();
        course.AddModule(BuildModule(course.Id, 1, withMaterial: false));

        var ex = Assert.Throws<InvalidOperationException>(() => course.Publish());

        Assert.Equal("Module phải có ít nhất một learning material.", ex.Message);
        Assert.False(course.IsPublished);
    }

    /// <summary>UTCID05 · Toàn bộ nhánh = F · Type B — đúng 1 học phần hợp lệ (biên dưới).</summary>
    [Fact]
    public void UTCID05_SingleValidModule_Publishes()
    {
        var course = BuildCourse();
        course.AddModule(BuildModule(course.Id, 1, withMaterial: true));

        course.Publish();

        Assert.True(course.IsPublished);
        Assert.False(course.IsPublicationLocked);
        Assert.NotNull(course.UpdatedAt);
    }

    /// <summary>UTCID06 · B4 nhiều vòng lặp · Type N — nhiều học phần đều hợp lệ.</summary>
    [Fact]
    public void UTCID06_MultipleValidModules_Publishes()
    {
        var course = BuildCourse();
        course.AddModule(BuildModule(course.Id, 1, withMaterial: true));
        course.AddModule(BuildModule(course.Id, 2, withMaterial: true));
        course.AddModule(BuildModule(course.Id, 3, withMaterial: true));

        course.Publish();

        Assert.True(course.IsPublished);
        Assert.Equal(3, course.Modules.Count);
    }
}
