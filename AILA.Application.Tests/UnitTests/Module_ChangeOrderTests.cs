using AILA.Domain.Entities;

namespace AILA.Application.Tests.UnitTests;

/// <summary>
/// Sheet: UT20_ModuleChangeOrder — <see cref="Module.ChangeOrder"/>
/// Module: Course · CC = 4 · 7 test case
///
/// Nhánh: B1 = newOrderIndex &lt;= 0 · B2 = newOrderIndex &gt; 999 · B3 = trùng thứ tự hiện tại
/// Miền hợp lệ [1 .. 999] ⇒ BVA đầy đủ: 0 / 1 / 999 / 1000.
/// </summary>
public class UT20_Module_ChangeOrderTests
{
    private static Module BuildModule(int orderIndex = 5) =>
        new(Guid.NewGuid(), "Học phần mở đầu", orderIndex);

    /// <summary>UTCID01 · B1=T · Type B — biên dưới không hợp lệ (0).</summary>
    [Fact]
    public void UTCID01_OrderIndexZero_ThrowsArgumentException()
    {
        var module = BuildModule();

        var ex = Assert.Throws<ArgumentException>(() => module.ChangeOrder(0));

        Assert.Contains("Vị trí sắp xếp phải nằm trong khoảng từ 1 đến 999.", ex.Message);
        Assert.Equal(5, module.OrderIndex);
    }

    /// <summary>UTCID02 · B1=T · Type A — giá trị âm.</summary>
    [Fact]
    public void UTCID02_NegativeOrderIndex_ThrowsArgumentException()
    {
        var module = BuildModule();

        Assert.Throws<ArgumentException>(() => module.ChangeOrder(-1));
        Assert.Equal(5, module.OrderIndex);
    }

    /// <summary>UTCID03 · B1=F, B2=T · Type B — biên trên không hợp lệ (1000).</summary>
    [Fact]
    public void UTCID03_OrderIndex1000_ThrowsArgumentException()
    {
        var module = BuildModule();

        Assert.Throws<ArgumentException>(() => module.ChangeOrder(1000));
        Assert.Equal(5, module.OrderIndex);
    }

    /// <summary>UTCID04 · B1=F, B2=F, B3=F · Type B — biên dưới hợp lệ (1).</summary>
    [Fact]
    public void UTCID04_OrderIndexExactly1_Succeeds()
    {
        var module = BuildModule();

        module.ChangeOrder(1);

        Assert.Equal(1, module.OrderIndex);
        Assert.NotNull(module.UpdatedAt);
    }

    /// <summary>UTCID05 · B2=F · Type B — biên trên hợp lệ (999).</summary>
    [Fact]
    public void UTCID05_OrderIndexExactly999_Succeeds()
    {
        var module = BuildModule();

        module.ChangeOrder(999);

        Assert.Equal(999, module.OrderIndex);
    }

    /// <summary>
    /// UTCID06 · B3=T · Type A — đổi sang đúng thứ tự đang có: return im lặng.
    /// Phải khẳng định UpdatedAt VẪN null để chứng minh không có thao tác ghi thừa.
    /// </summary>
    [Fact]
    public void UTCID06_SameOrderIndex_ReturnsWithoutTouchingTimestamp()
    {
        var module = BuildModule(orderIndex: 5);

        module.ChangeOrder(5);

        Assert.Equal(5, module.OrderIndex);
        Assert.Null(module.UpdatedAt);
    }

    /// <summary>UTCID07 · Toàn bộ nhánh = F · Type N — đổi thứ tự thông thường.</summary>
    [Fact]
    public void UTCID07_ValidNewOrderIndex_UpdatesOrderAndTimestamp()
    {
        var module = BuildModule(orderIndex: 5);

        module.ChangeOrder(10);

        Assert.Equal(10, module.OrderIndex);
        Assert.NotNull(module.UpdatedAt);
    }
}
