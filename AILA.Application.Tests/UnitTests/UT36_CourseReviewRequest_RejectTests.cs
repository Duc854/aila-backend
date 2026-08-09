using AILA.Domain.Entities;
using AILA.Domain.Enums;

namespace AILA.Application.Tests.UnitTests;

/// <summary>
/// Sheet: UT36_RejectCourseReview — <see cref="CourseReviewRequest.Reject"/>
/// Module: Moderation · CC = 3 · 6 test case
///
/// Nhánh: B1 = Status != Pending (throw InvalidOperationException)
///        B2 = reviewComment rỗng/khoảng trắng (throw ArgumentException)
///
/// B1 vừa chặn double-submit vừa bảo vệ ReviewComment/ReviewedAt của lần xử lý đầu
/// khỏi bị ghi đè khi Admin bấm nhầm lần hai — xem UTCID03.
/// Khác với <see cref="CourseReviewRequest.Approve"/>, ở đây lý do là BẮT BUỘC:
/// Expert cần biết vì sao khoá học của mình không được mở lại.
/// </summary>
public class UT36_CourseReviewRequest_RejectTests
{
    private static CourseReviewRequest BuildPending() =>
        new(Guid.NewGuid(), "Khoá học bị khoá do báo cáo spam, xin xem xét lại");

    /// <summary>UTCID01 · B1=F, B2=F · Type N — từ chối yêu cầu đang chờ, lý do được Trim.</summary>
    [Fact]
    public void UTCID01_PendingRequest_RejectsAndTrimsComment()
    {
        var request = BuildPending();

        request.Reject("  Nội dung vẫn vi phạm chính sách  ");

        Assert.Equal(CourseReviewRequestStatus.Rejected, request.Status);
        Assert.Equal("Nội dung vẫn vi phạm chính sách", request.ReviewComment);
        Assert.NotNull(request.ReviewedAt);
        Assert.NotNull(request.UpdatedAt);
    }

    /// <summary>UTCID02 · B1=T · Type A — yêu cầu đã được duyệt trước đó.</summary>
    [Fact]
    public void UTCID02_ApprovedRequest_ThrowsInvalidOperation()
    {
        var request = BuildPending();
        request.Approve("Đã xử lý xong");

        var ex = Assert.Throws<InvalidOperationException>(
            () => request.Reject("Lý do bất kỳ"));

        Assert.Equal("Yêu cầu đã được xử lý.", ex.Message);
    }

    /// <summary>
    /// UTCID03 · B1=T (che B2) · Type A — Admin bấm từ chối lần thứ hai.
    /// Chứng minh B1 chặn trước B2 và lý do lần đầu KHÔNG bị ghi đè.
    /// </summary>
    [Fact]
    public void UTCID03_RejectedTwice_ThrowsAndKeepsFirstComment()
    {
        var request = BuildPending();
        request.Reject("Lý do lần đầu");
        var firstReviewedAt = request.ReviewedAt;

        Assert.Throws<InvalidOperationException>(() => request.Reject(""));

        Assert.Equal("Lý do lần đầu", request.ReviewComment);
        Assert.Equal(firstReviewedAt, request.ReviewedAt);
    }

    /// <summary>UTCID04 · B1=F, B2=T · Type A — lý do null.</summary>
    [Fact]
    public void UTCID04_NullComment_ThrowsArgumentException()
    {
        var request = BuildPending();

        Assert.Throws<ArgumentException>(() => request.Reject(null!));
        Assert.Equal(CourseReviewRequestStatus.Pending, request.Status);
    }

    /// <summary>UTCID05 · B2=T · Type B — lý do là chuỗi rỗng.</summary>
    [Fact]
    public void UTCID05_EmptyComment_ThrowsArgumentException()
    {
        var request = BuildPending();

        Assert.Throws<ArgumentException>(() => request.Reject(""));
        Assert.Equal(CourseReviewRequestStatus.Pending, request.Status);
    }

    /// <summary>UTCID06 · B2=T · Type B — lý do chỉ gồm khoảng trắng.</summary>
    [Fact]
    public void UTCID06_WhitespaceOnlyComment_ThrowsArgumentException()
    {
        var request = BuildPending();

        Assert.Throws<ArgumentException>(() => request.Reject("   "));
        Assert.Null(request.ReviewedAt);
    }
}
