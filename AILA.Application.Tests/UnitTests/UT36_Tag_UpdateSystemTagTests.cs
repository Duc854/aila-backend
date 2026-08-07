using AILA.Domain.Entities;

namespace AILA.Application.Tests.UnitTests;

/// <summary>
/// Sheet: UT36_UpdateSystemTag — <see cref="Tag.UpdateSystemTag"/>
/// Module: Tag · CC = 4 · 6 test case
///
/// Nhánh: B1 = CreatedById != null (throw — chỉ System Tag mới được sửa)
///        B2 = name rỗng · B3 = code rỗng
///
/// B1 là hàng rào phân quyền dữ liệu: Custom Tag do Expert tạo (CreatedById != null)
/// không được Admin sửa trực tiếp qua API System Tag.
/// Code được chuẩn hoá: Trim + lowercase + thay khoảng trắng bằng dấu gạch ngang.
/// </summary>
public class UT36_Tag_UpdateSystemTagTests
{
    private static Tag SystemTag() => Tag.CreateByAdmin("Prompt Basic", "prompt-basic");

    private static Tag CustomTag() => Tag.CreateByExpert("Tag Cua Expert", "tag-cua-expert", Guid.NewGuid());

    /// <summary>UTCID01 · Toàn bộ nhánh = F · Type N — cập nhật System Tag, code được chuẩn hoá.</summary>
    [Fact]
    public void UTCID01_SystemTagWithValidInput_UpdatesAndNormalizesCode()
    {
        var tag = SystemTag();

        tag.UpdateSystemTag("  AI Foundation  ", "  AI Foundation  ");

        Assert.Equal("AI Foundation", tag.Name);
        Assert.Equal("ai-foundation", tag.Code);
        Assert.NotNull(tag.UpdatedAt);
    }

    /// <summary>UTCID02 · B1=T · Type A — Custom Tag do Expert tạo, không được sửa.</summary>
    [Fact]
    public void UTCID02_CustomTag_ThrowsInvalidOperation()
    {
        var tag = CustomTag();

        var ex = Assert.Throws<InvalidOperationException>(
            () => tag.UpdateSystemTag("Ten moi", "ten-moi"));

        Assert.Equal("Chỉ System Tag mới được cập nhật.", ex.Message);
        Assert.Equal("Tag Cua Expert", tag.Name);
    }

    /// <summary>UTCID03 · B1=F, B2=T · Type A — tên null.</summary>
    [Fact]
    public void UTCID03_NullName_ThrowsArgumentException()
    {
        var tag = SystemTag();

        var ex = Assert.Throws<ArgumentException>(() => tag.UpdateSystemTag(null!, "ma-moi"));

        Assert.Contains("Tên tag không được để trống.", ex.Message);
        Assert.Equal("Prompt Basic", tag.Name);
    }

    /// <summary>UTCID04 · B2=T · Type B — tên toàn khoảng trắng (biên).</summary>
    [Fact]
    public void UTCID04_WhitespaceName_ThrowsArgumentException()
    {
        var tag = SystemTag();

        Assert.Throws<ArgumentException>(() => tag.UpdateSystemTag("   ", "ma-moi"));
        Assert.Equal("Prompt Basic", tag.Name);
    }

    /// <summary>UTCID05 · B2=F, B3=T · Type A — code null. Tên phải hợp lệ để thoát B2.</summary>
    [Fact]
    public void UTCID05_NullCode_ThrowsArgumentException()
    {
        var tag = SystemTag();

        var ex = Assert.Throws<ArgumentException>(() => tag.UpdateSystemTag("Ten moi", null!));

        Assert.Contains("Code tag không được để trống.", ex.Message);
        Assert.Equal("prompt-basic", tag.Code);
    }

    /// <summary>UTCID06 · B3=T · Type B — code toàn khoảng trắng (biên).</summary>
    [Fact]
    public void UTCID06_WhitespaceCode_ThrowsArgumentException()
    {
        var tag = SystemTag();

        Assert.Throws<ArgumentException>(() => tag.UpdateSystemTag("Ten moi", "   "));
        Assert.Equal("prompt-basic", tag.Code);
    }
}
