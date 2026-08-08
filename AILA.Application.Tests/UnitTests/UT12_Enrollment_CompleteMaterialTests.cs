using AILA.Domain.Entities;
using AILA.Domain.Enums;

namespace AILA.Application.Tests.UnitTests;

/// <summary>
/// Sheet: UT12_CompleteMaterial — <see cref="Enrollment.CompleteMaterial"/>
/// Module: Learning · CC = 6 (4 đường khả thi) · 6 test case
///
/// Nhánh: B1 = Status == Completed (RETURN IM LẶNG) · B2 = CompletedMaterials &gt;= TotalMaterials (THROW)
///        CalculateProgress: B3 = TotalMaterials == 0 · B4/B5 = hoàn thành hết &amp;&amp; chưa Completed
///
/// Nhánh INFEASIBLE (đã ghi nhận ở Test Analysis §7):
///  - B3: TotalMaterials == 0 luôn bị B2 ném exception chặn trước (xem UTCID06).
///  - B5 = F: B1 đã loại trạng thái Completed nên trong CompleteMaterial() B5 luôn = T khi B4 = T.
/// </summary>
public class UT12_Enrollment_CompleteMaterialTests
{
    private static Enrollment BuildEnrollment(int totalMaterials, int completedMaterials = 0)
    {
        var enrollment = new Enrollment(Guid.NewGuid(), Guid.NewGuid(), totalMaterials);

        for (var i = 0; i < completedMaterials; i++)
            enrollment.CompleteMaterial();

        return enrollment;
    }

    /// <summary>UTCID01 · B1=F, B2=F, B4=F · Type N — hoàn thành học liệu đầu tiên trong 3.</summary>
    [Fact]
    public void UTCID01_CompleteFirstOfThree_ProgressIs33Point33()
    {
        var enrollment = BuildEnrollment(totalMaterials: 3);

        enrollment.CompleteMaterial();

        Assert.Equal(1, enrollment.CompletedMaterials);
        Assert.Equal(33.33m, enrollment.ProgressPct);
        Assert.Equal(EnrollmentStatus.Active, enrollment.Status);
        Assert.Null(enrollment.CompletedAt);
        Assert.NotNull(enrollment.LastAccessedAt);
    }

    /// <summary>UTCID02 · B4=F · Type N — hoàn thành học liệu thứ hai trong 3.</summary>
    [Fact]
    public void UTCID02_CompleteSecondOfThree_ProgressIs66Point67()
    {
        var enrollment = BuildEnrollment(totalMaterials: 3, completedMaterials: 1);

        enrollment.CompleteMaterial();

        Assert.Equal(2, enrollment.CompletedMaterials);
        Assert.Equal(66.67m, enrollment.ProgressPct);
        Assert.Equal(EnrollmentStatus.Active, enrollment.Status);
    }

    /// <summary>UTCID03 · B4=T, B5=T · Type B — hoàn thành học liệu cuối ⇒ tự chuyển trạng thái Completed.</summary>
    [Fact]
    public void UTCID03_CompleteLastOfThree_TransitionsToCompleted()
    {
        var enrollment = BuildEnrollment(totalMaterials: 3, completedMaterials: 2);

        enrollment.CompleteMaterial();

        Assert.Equal(3, enrollment.CompletedMaterials);
        Assert.Equal(100.00m, enrollment.ProgressPct);
        Assert.Equal(EnrollmentStatus.Completed, enrollment.Status);
        Assert.NotNull(enrollment.CompletedAt);
    }

    /// <summary>UTCID04 · B4=T · Type B — khoá học chỉ có 1 học liệu (biên dưới hợp lệ của TotalMaterials).</summary>
    [Fact]
    public void UTCID04_SingleMaterialCourse_CompletesImmediately()
    {
        var enrollment = BuildEnrollment(totalMaterials: 1);

        enrollment.CompleteMaterial();

        Assert.Equal(1, enrollment.CompletedMaterials);
        Assert.Equal(100.00m, enrollment.ProgressPct);
        Assert.Equal(EnrollmentStatus.Completed, enrollment.Status);
    }

    /// <summary>
    /// UTCID05 · B1=T · Type A — gọi lại trên enrollment ĐÃ hoàn thành: return im lặng.
    /// Phải assert TRẠNG THÁI GIỮ NGUYÊN, không chỉ assert "không ném exception" —
    /// nếu chỉ kiểm exception thì lỗi tăng quá tổng số học liệu vẫn lọt.
    /// </summary>
    [Fact]
    public void UTCID05_AlreadyCompletedEnrollment_ReturnsSilentlyWithoutChangingState()
    {
        var enrollment = BuildEnrollment(totalMaterials: 3, completedMaterials: 3);
        var completedAt = enrollment.CompletedAt;

        enrollment.CompleteMaterial();

        Assert.Equal(3, enrollment.CompletedMaterials);
        Assert.Equal(100.00m, enrollment.ProgressPct);
        Assert.Equal(EnrollmentStatus.Completed, enrollment.Status);
        Assert.Equal(completedAt, enrollment.CompletedAt);
    }

    /// <summary>
    /// UTCID06 · B1=F, B2=T · Type A — khoá học không có học liệu nào (TotalMaterials = 0).
    /// Là test case DUY NHẤT chạm được câu lệnh throw.
    /// Đồng thời chứng minh nhánh CalculateProgress B3 (TotalMaterials == 0) là INFEASIBLE
    /// từ entry point này: exception được ném trước khi tới CalculateProgress.
    /// </summary>
    [Fact]
    public void UTCID06_CourseWithoutMaterials_ThrowsInvalidOperationException()
    {
        var enrollment = BuildEnrollment(totalMaterials: 0);

        var ex = Assert.Throws<InvalidOperationException>(() => enrollment.CompleteMaterial());

        Assert.Equal(
            "Số học liệu hoàn thành không thể vượt quá tổng số học liệu của khóa học.",
            ex.Message);
        Assert.Equal(0, enrollment.CompletedMaterials);
        Assert.Equal(0.00m, enrollment.ProgressPct);
        Assert.Equal(EnrollmentStatus.Active, enrollment.Status);
    }
}
