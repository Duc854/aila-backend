using AILA.Domain.Entities;
using AILA.Domain.Enums;

namespace AILA.Application.Tests.UnitTests;

/// <summary>
/// Sheet: UT23_UpdateTotalMaterials — <see cref="Enrollment.UpdateTotalMaterials"/>
/// Module: Learning · CC = 5 · 6 test case
///
/// Nhánh: B1 = newTotal &lt; CompletedMaterials (throw "không được nhỏ hơn số đã hoàn thành")
///        B2 = newTotal &lt; TotalMaterials     (throw "không thể giảm")
///        CalculateProgress: B3 = TotalMaterials == 0 · B4/B5 = hoàn thành hết &amp;&amp; chưa Completed
///
/// Vì CompletedMaterials &lt;= TotalMaterials là bất biến của entity nên B1 ⇒ B2:
/// hai nhánh throw được phân tách bằng vùng CompletedMaterials &lt;= newTotal &lt; TotalMaterials
/// (UTCID02 chạm B2, UTCID03 chạm B1).
///
/// Nhánh INFEASIBLE (đã ghi nhận ở Test Analysis §7):
///  - B4 = T: cần CompletedMaterials == newTotal, mà B2 buộc newTotal &gt;= TotalMaterials và
///    bất biến buộc CompletedMaterials &lt;= TotalMaterials ⇒ cả ba bằng nhau. Khi đó enrollment
///    đã được CompleteMaterial() chuyển sang Completed từ trước (B5 = F, xem UTCID04),
///    hoặc TotalMaterials == 0 và bị B3 return sớm. Chuyển trạng thái Completed giờ chỉ
///    còn đi qua CompleteMaterial() — phủ ở UT12/UTCID03.
///
/// Điểm quan trọng: nhánh B3 (TotalMaterials == 0) là INFEASIBLE từ CompleteMaterial()
/// (xem UT12) nhưng CHẠM ĐƯỢC từ đây — UTCID05 là test case duy nhất phủ nó trong cả bộ.
/// </summary>
public class UT23_Enrollment_UpdateTotalMaterialsTests
{
    private static Enrollment BuildEnrollment(int totalMaterials, int completedMaterials = 0)
    {
        var enrollment = new Enrollment(Guid.NewGuid(), Guid.NewGuid(), totalMaterials);
        for (var i = 0; i < completedMaterials; i++)
            enrollment.CompleteMaterial();
        return enrollment;
    }

    /// <summary>UTCID01 · B1=F, B2=F, B4=F · Type N — Expert thêm bài mới (3 → 5).</summary>
    [Fact]
    public void UTCID01_IncreaseTotal_RecalculatesProgress()
    {
        var enrollment = BuildEnrollment(totalMaterials: 3, completedMaterials: 1);

        enrollment.UpdateTotalMaterials(5);

        Assert.Equal(5, enrollment.TotalMaterials);
        Assert.Equal(1, enrollment.CompletedMaterials);
        Assert.Equal(20.00m, enrollment.ProgressPct);
        Assert.Equal(EnrollmentStatus.Active, enrollment.Status);
    }

    /// <summary>
    /// UTCID02 · B1=F, B2=T · Type A — Expert xóa bớt bài (3 → 2) trong khi học viên mới
    /// hoàn thành 1 bài: tổng số học liệu KHÔNG được phép giảm, tiến độ giữ nguyên.
    /// </summary>
    [Fact]
    public void UTCID02_DecreaseTotal_ThrowsArgumentException()
    {
        var enrollment = BuildEnrollment(totalMaterials: 3, completedMaterials: 1);

        var ex = Assert.Throws<ArgumentException>(() => enrollment.UpdateTotalMaterials(2));

        Assert.Contains("Tổng số học liệu không thể giảm.", ex.Message);
        Assert.Equal(3, enrollment.TotalMaterials);
        Assert.Equal(33.33m, enrollment.ProgressPct);
        Assert.Equal(EnrollmentStatus.Active, enrollment.Status);
    }

    /// <summary>
    /// UTCID03 · B1=T · Type A — tổng mới nhỏ hơn số bài đã hoàn thành.
    /// Cả B1 và B2 đều đúng, nhưng B1 được kiểm trước nên phải nhận thông báo của B1.
    /// </summary>
    [Fact]
    public void UTCID03_NewTotalBelowCompleted_ThrowsArgumentException()
    {
        var enrollment = BuildEnrollment(totalMaterials: 3, completedMaterials: 2);

        var ex = Assert.Throws<ArgumentException>(() => enrollment.UpdateTotalMaterials(1));

        Assert.Contains(
            "Tổng số học liệu mới không được nhỏ hơn số học liệu đã hoàn thành.",
            ex.Message);
        Assert.Equal(3, enrollment.TotalMaterials);
    }

    /// <summary>
    /// UTCID04 · B1=F, B2=F (biên), B4=T, B5=F · Type B — enrollment ĐÃ hoàn thành, cập nhật
    /// đúng bằng tổng cũ (3 → 3). Chạm B4=T nhưng B5=F vì Status đã là Completed:
    /// thao tác phải BẤT BIẾN, đặc biệt KHÔNG được ghi đè CompletedAt.
    /// </summary>
    [Fact]
    public void UTCID04_CompletedEnrollmentSameTotal_KeepsCompletedAtUnchanged()
    {
        var enrollment = BuildEnrollment(totalMaterials: 3, completedMaterials: 3);
        var completedAt = enrollment.CompletedAt;

        enrollment.UpdateTotalMaterials(3);

        Assert.Equal(3, enrollment.TotalMaterials);
        Assert.Equal(100.00m, enrollment.ProgressPct);
        Assert.Equal(EnrollmentStatus.Completed, enrollment.Status);
        Assert.Equal(completedAt, enrollment.CompletedAt);
    }

    /// <summary>
    /// UTCID05 · B1=F, B2=F, B3=T · Type B — khóa học chưa có học liệu nào (TotalMaterials = 0).
    /// Là test case DUY NHẤT trong cả bộ phủ được nhánh CalculateProgress B3 —
    /// từ CompleteMaterial() nhánh này bị chặn bởi throw (xem Test Analysis §7).
    /// </summary>
    [Fact]
    public void UTCID05_TotalStaysZero_SetsProgressToZero()
    {
        var enrollment = BuildEnrollment(totalMaterials: 0);

        enrollment.UpdateTotalMaterials(0);

        Assert.Equal(0, enrollment.TotalMaterials);
        Assert.Equal(0, enrollment.CompletedMaterials);
        Assert.Equal(0.00m, enrollment.ProgressPct);
        Assert.Equal(EnrollmentStatus.Active, enrollment.Status);
    }

    /// <summary>UTCID06 · B1=F, B2=F, B4=F · Type B — tổng không đổi, tiến độ giữ nguyên.</summary>
    [Fact]
    public void UTCID06_SameTotal_KeepsProgress()
    {
        var enrollment = BuildEnrollment(totalMaterials: 3, completedMaterials: 1);

        enrollment.UpdateTotalMaterials(3);

        Assert.Equal(3, enrollment.TotalMaterials);
        Assert.Equal(33.33m, enrollment.ProgressPct);
    }
}
