using AILA.Domain.Entities;

namespace AILA.Application.Tests.UnitTests;

/// <summary>
/// Sheet: UT21_ValidateBeforePublish — <see cref="Module.ValidateBeforeCoursePublish"/>
/// Module: Course · CC = 2 · 3 test case
///
/// Nhánh: B1 = !_materials.Any() (throw)
/// Đây là hàng rào cuối được Course.Publish() gọi lan truyền cho từng học phần (xem UT16).
/// </summary>
public class UT21_Module_ValidateBeforeCoursePublishTests
{
    private static Module BuildModule(int materialCount)
    {
        var module = new Module(Guid.NewGuid(), "Học phần mở đầu", 1);
        for (var i = 0; i < materialCount; i++)
            module.AddMaterial(Material.CreateVideo(module.Id, $"Bài học số {i + 1}", i + 1));
        return module;
    }

    /// <summary>UTCID01 · B1=T · Type A — học phần rỗng, không có học liệu nào.</summary>
    [Fact]
    public void UTCID01_WithoutMaterial_ThrowsInvalidOperation()
    {
        var module = BuildModule(materialCount: 0);

        var ex = Assert.Throws<InvalidOperationException>(() => module.ValidateBeforeCoursePublish());

        Assert.Equal("Module phải có ít nhất một learning material.", ex.Message);
        Assert.Null(module.UpdatedAt);
    }

    /// <summary>UTCID02 · B1=F · Type B — đúng 1 học liệu (biên dưới hợp lệ).</summary>
    [Fact]
    public void UTCID02_ExactlyOneMaterial_PassesValidation()
    {
        var module = BuildModule(materialCount: 1);

        module.ValidateBeforeCoursePublish();

        Assert.Single(module.Materials);
        Assert.NotNull(module.UpdatedAt);
    }

    /// <summary>UTCID03 · B1=F · Type N — nhiều học liệu.</summary>
    [Fact]
    public void UTCID03_MultipleMaterials_PassesValidation()
    {
        var module = BuildModule(materialCount: 3);

        module.ValidateBeforeCoursePublish();

        Assert.Equal(3, module.Materials.Count);
        Assert.NotNull(module.UpdatedAt);
    }
}
