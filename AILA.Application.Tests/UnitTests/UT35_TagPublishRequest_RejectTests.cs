using AILA.Domain.Entities;
using AILA.Domain.Enums;

namespace AILA.Application.Tests.UnitTests;

/// <summary>
/// Sheet: UT35_RejectTagRequest — <see cref="TagPublishRequest.Reject"/>
/// Module: Tag · CC = 3 · 6 test case
///
/// Nhánh: B1 = EnsurePending() — Status != Pending (throw InvalidOperationException)
///        B2 = reviewComment rỗng/khoảng trắng (throw ArgumentException)
///
/// Thứ tự kiểm tra là điểm white-box đáng khoá lại: B1 chạy TRƯỚC B2, nên một yêu cầu
/// đã xử lý mà lại truyền lý do rỗng vẫn phải ném InvalidOperationException — xem UTCID03.
/// </summary>
public class UT35_TagPublishRequest_RejectTests
{
    private static readonly Guid TagId = Guid.NewGuid();
    private static readonly Guid OwnerId = Guid.NewGuid();

    private static TagPublishRequest BuildPending() =>
        TagPublishRequest.Create(TagId, OwnerId, "Tag phục vụ khoá Prompt cơ bản");

    /// <summary>UTCID01 · B1=F, B2=F · Type N — từ chối yêu cầu đang chờ, lý do được Trim.</summary>
    [Fact]
    public void UTCID01_PendingRequest_RejectsAndTrimsComment()
    {
        var request = BuildPending();

        request.Reject("  Tên tag trùng tag hệ thống  ");

        Assert.Equal(TagPublishRequestStatus.Rejected, request.Status);
        Assert.Equal("Tên tag trùng tag hệ thống", request.ReviewComment);
        Assert.NotNull(request.ReviewedAt);
        Assert.NotNull(request.UpdatedAt);
    }

    /// <summary>UTCID02 · B1=T · Type A — yêu cầu đã được duyệt.</summary>
    [Fact]
    public void UTCID02_ApprovedRequest_ThrowsInvalidOperation()
    {
        var request = BuildPending();
        request.Approve();

        var ex = Assert.Throws<InvalidOperationException>(
            () => request.Reject("Lý do bất kỳ"));

        Assert.Equal("Yêu cầu này đã được xử lý.", ex.Message);
    }

    /// <summary>
    /// UTCID03 · B1=T (che B2) · Type A — yêu cầu đã bị từ chối VÀ lý do rỗng.
    /// Chứng minh B1 chặn trước: ném InvalidOperationException chứ không phải ArgumentException,
    /// đồng thời lý do từ chối lần đầu KHÔNG bị ghi đè.
    /// </summary>
    [Fact]
    public void UTCID03_AlreadyRejectedWithEmptyComment_ThrowsInvalidOperationNotArgument()
    {
        var request = BuildPending();
        request.Reject("Lý do lần đầu");

        Assert.Throws<InvalidOperationException>(() => request.Reject(""));
        Assert.Equal("Lý do lần đầu", request.ReviewComment);
    }

    /// <summary>UTCID04 · B1=F, B2=T · Type A — lý do null.</summary>
    [Fact]
    public void UTCID04_NullComment_ThrowsArgumentException()
    {
        var request = BuildPending();

        Assert.Throws<ArgumentException>(() => request.Reject(null!));
        Assert.Equal(TagPublishRequestStatus.Pending, request.Status);
    }

    /// <summary>UTCID05 · B2=T · Type B — lý do là chuỗi rỗng.</summary>
    [Fact]
    public void UTCID05_EmptyComment_ThrowsArgumentException()
    {
        var request = BuildPending();

        Assert.Throws<ArgumentException>(() => request.Reject(""));
        Assert.Equal(TagPublishRequestStatus.Pending, request.Status);
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
