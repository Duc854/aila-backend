using AILA.Domain.Entities;
using AILA.Domain.Enums;

namespace AILA.Application.Tests.UnitTests;

/// <summary>
/// Sheet: UT23_UpdateTotalMaterials — <see cref="Enrollment.UpdateTotalMaterials"/>
/// Module: Learning · CC = 5 · 6 test case
///
/// Nhánh: B1 = newTotal &lt; CompletedMaterials (throw)
///        CalculateProgress: B2 = TotalMaterials == 0 · B3/B4 = hoàn thành hết &amp;&amp; chưa Completed
///
/// Điểm quan trọng: nhánh B2 (TotalMaterials == 0) là INFEASIBLE từ CompleteMaterial()
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

    /// <summary>UTCID01 · B1=F, B3=F · Type N — Expert thêm bài mới (3 → 5).</summary>
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

    /// <summary>UTCID02 · B1=F, B3=F · Type N — Expert xóa bớt bài (3 → 2).</summary>
    [Fact]
    public void UTCID02_DecreaseTotal_RecalculatesProgress()
    {
        var enrollment = BuildEnrollment(totalMaterials: 3, completedMaterials: 1);

        enrollment.UpdateTotalMaterials(2);

        Assert.Equal(2, enrollment.TotalMaterials);
        Assert.Equal(50.00m, enrollment.ProgressPct);
        Assert.Equal(EnrollmentStatus.Active, enrollment.Status);
    }

    /// <summary>UTCID03 · B1=T · Type A — tổng mới nhỏ hơn số bài đã hoàn thành.</summary>
    [Fact]
    public void UTCID03_NewTotalBelowCompleted_ThrowsArgumentException()
    {
        var enrollment = BuildEnrollment(totalMaterials: 3, completedMaterials: 2);

        var ex = Assert.Throws<ArgumentException>(() => enrollment.UpdateTotalMaterials(1));

        Assert.Contains(
            "Tổng số học liệu mới không được nhỏ hơn số học liệu học viên đã hoàn thành.",
            ex.Message);
        Assert.Equal(3, enrollment.TotalMaterials);
    }

    /// <summary>
    /// UTCID04 · B1=F (biên), B3=T, B4=T · Type B — tổng mới BẰNG ĐÚNG số bài đã hoàn thành
    /// ⇒ tự chuyển sang Completed.
    /// </summary>
    [Fact]
    public void UTCID04_NewTotalEqualsCompleted_TransitionsToCompleted()
    {
        var enrollment = BuildEnrollment(totalMaterials: 3, completedMaterials: 2);

        enrollment.UpdateTotalMaterials(2);

        Assert.Equal(2, enrollment.TotalMaterials);
        Assert.Equal(100.00m, enrollment.ProgressPct);
        Assert.Equal(EnrollmentStatus.Completed, enrollment.Status);
        Assert.NotNull(enrollment.CompletedAt);
    }

    /// <summary>
    /// UTCID05 · B2=T · Type B — khóa học bị xóa hết học liệu (TotalMaterials = 0).
    /// Là test case DUY NHẤT trong cả bộ phủ được nhánh CalculateProgress B2 —
    /// từ CompleteMaterial() nhánh này bị chặn bởi throw (xem Test Analysis §7).
    /// </summary>
    [Fact]
    public void UTCID05_NewTotalZero_SetsProgressToZero()
    {
        var enrollment = BuildEnrollment(totalMaterials: 3, completedMaterials: 0);

        enrollment.UpdateTotalMaterials(0);

        Assert.Equal(0, enrollment.TotalMaterials);
        Assert.Equal(0.00m, enrollment.ProgressPct);
        Assert.Equal(EnrollmentStatus.Active, enrollment.Status);
    }

    /// <summary>UTCID06 · B1=F, B3=F · Type B — tổng không đổi, tiến độ giữ nguyên.</summary>
    [Fact]
    public void UTCID06_SameTotal_KeepsProgress()
    {
        var enrollment = BuildEnrollment(totalMaterials: 3, completedMaterials: 1);

        enrollment.UpdateTotalMaterials(3);

        Assert.Equal(3, enrollment.TotalMaterials);
        Assert.Equal(33.33m, enrollment.ProgressPct);
    }
}
