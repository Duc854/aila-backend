using AILA.Domain.Entities;
using AILA.Domain.Enums;

namespace AILA.Application.Tests.UnitTests;

/// <summary>
/// Sheet: UT27_UncompleteMaterial — <see cref="Enrollment.UncompleteMaterial"/>
/// Module: Learning · CC = 5 · 6 test case
///
/// Nhánh: B1 = CompletedMaterials &lt;= 0 (return im lặng)
///        B2 = Status == Completed (hạ về Active, xoá CompletedAt)
///        CalculateProgress: B3 = TotalMaterials == 0 · B4/B5 = hoàn thành hết
///
/// B2 là nhánh nghiệp vụ quan trọng: học viên học lại một bài sau khi đã hoàn thành khóa
/// ⇒ khóa học phải quay về Active và CompletedAt phải bị xoá.
/// </summary>
public class UT27_Enrollment_UncompleteMaterialTests
{
    private static Enrollment BuildEnrollment(int totalMaterials, int completedMaterials = 0)
    {
        var enrollment = new Enrollment(Guid.NewGuid(), Guid.NewGuid(), totalMaterials);
        for (var i = 0; i < completedMaterials; i++)
            enrollment.CompleteMaterial();
        return enrollment;
    }

    /// <summary>UTCID01 · B1=T · Type A — chưa hoàn thành bài nào: return im lặng.</summary>
    [Fact]
    public void UTCID01_NothingCompleted_ReturnsSilently()
    {
        var enrollment = BuildEnrollment(totalMaterials: 3);

        enrollment.UncompleteMaterial();

        Assert.Equal(0, enrollment.CompletedMaterials);
        Assert.Equal(0.00m, enrollment.ProgressPct);
        Assert.Equal(EnrollmentStatus.Active, enrollment.Status);
    }

    /// <summary>UTCID02 · B1=F, B2=F · Type N — lùi 1 bài khi đang học dở.</summary>
    [Fact]
    public void UTCID02_ActiveEnrollment_DecrementsAndRecalculates()
    {
        var enrollment = BuildEnrollment(totalMaterials: 3, completedMaterials: 2);

        enrollment.UncompleteMaterial();

        Assert.Equal(1, enrollment.CompletedMaterials);
        Assert.Equal(33.33m, enrollment.ProgressPct);
        Assert.Equal(EnrollmentStatus.Active, enrollment.Status);
    }

    /// <summary>
    /// UTCID03 · B2=T · Type B — lùi bài khi khóa học ĐÃ hoàn thành
    /// ⇒ hạ trạng thái về Active và xoá CompletedAt.
    /// </summary>
    [Fact]
    public void UTCID03_CompletedEnrollment_DowngradesToActiveAndClearsCompletedAt()
    {
        var enrollment = BuildEnrollment(totalMaterials: 3, completedMaterials: 3);
        Assert.Equal(EnrollmentStatus.Completed, enrollment.Status);
        Assert.NotNull(enrollment.CompletedAt);

        enrollment.UncompleteMaterial();

        Assert.Equal(2, enrollment.CompletedMaterials);
        Assert.Equal(66.67m, enrollment.ProgressPct);
        Assert.Equal(EnrollmentStatus.Active, enrollment.Status);
        Assert.Null(enrollment.CompletedAt);
    }

    /// <summary>UTCID04 · B2=T · Type B — khóa 1 bài: từ Completed lùi về 0%.</summary>
    [Fact]
    public void UTCID04_SingleMaterialCourse_GoesBackToZero()
    {
        var enrollment = BuildEnrollment(totalMaterials: 1, completedMaterials: 1);

        enrollment.UncompleteMaterial();

        Assert.Equal(0, enrollment.CompletedMaterials);
        Assert.Equal(0.00m, enrollment.ProgressPct);
        Assert.Equal(EnrollmentStatus.Active, enrollment.Status);
        Assert.Null(enrollment.CompletedAt);
    }

    /// <summary>UTCID05 · B1=F, B2=F · Type B — lùi bài cuối cùng còn lại về 0.</summary>
    [Fact]
    public void UTCID05_LastRemainingCompletedMaterial_GoesBackToZero()
    {
        var enrollment = BuildEnrollment(totalMaterials: 3, completedMaterials: 1);

        enrollment.UncompleteMaterial();

        Assert.Equal(0, enrollment.CompletedMaterials);
        Assert.Equal(0.00m, enrollment.ProgressPct);
    }

    /// <summary>UTCID06 · B2=T rồi B2=F · Type N — gọi hai lần liên tiếp từ trạng thái Completed.</summary>
    [Fact]
    public void UTCID06_CalledTwiceFromCompleted_DecrementsTwice()
    {
        var enrollment = BuildEnrollment(totalMaterials: 3, completedMaterials: 3);

        enrollment.UncompleteMaterial();
        enrollment.UncompleteMaterial();

        Assert.Equal(1, enrollment.CompletedMaterials);
        Assert.Equal(33.33m, enrollment.ProgressPct);
        Assert.Equal(EnrollmentStatus.Active, enrollment.Status);
    }
}
