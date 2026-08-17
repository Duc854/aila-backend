using AILA.Domain.Entities;
using AILA.Domain.Enums;

namespace AILA.Application.Tests.UnitTests;

/// <summary>
/// Sheet: UT34_ResubmitTagRequest — <see cref="TagPublishRequest.Resubmit"/>
/// Module: Tag · CC = 4 · 7 test case
///
/// Nhánh: B1 = Status != Rejected (throw) · B2 = requestedById rỗng (throw)
///        B3 = ternary chuẩn hoá RequestNote (rỗng/khoảng trắng ⇒ null, ngược lại Trim)
///
/// Bất biến nghiệp vụ: gửi lại phải XOÁ dấu vết lần duyệt trước
/// (ReviewComment = null, ReviewedAt = null), nếu không Admin sẽ đọc nhầm
/// lý do từ chối cũ khi xem yêu cầu mới.
/// </summary>
public class UT34_TagPublishRequest_ResubmitTests
{
    private static readonly Guid TagId = Guid.NewGuid();
    private static readonly Guid OwnerId = Guid.NewGuid();
    private static readonly Guid OtherExpertId = Guid.NewGuid();

    private static TagPublishRequest BuildPending() =>
        TagPublishRequest.Create(TagId, OwnerId, "Tag phục vụ khoá Prompt cơ bản");

    private static TagPublishRequest BuildRejected()
    {
        var request = BuildPending();
        request.Reject("Tên tag trùng với tag hệ thống");
        return request;
    }

    private static TagPublishRequest BuildApproved()
    {
        var request = BuildPending();
        request.Approve();
        return request;
    }

    /// <summary>
    /// UTCID01 · B1=F, B2=F, B3=F · Type N — gửi lại sau khi bị từ chối.
    /// Assert cả việc xoá ReviewComment/ReviewedAt, không chỉ Status.
    /// </summary>
    [Fact]
    public void UTCID01_RejectedRequestResubmitted_ReturnsToPendingAndClearsReview()
    {
        var request = BuildRejected();

        request.Resubmit(OwnerId, "Đã đổi tên tag theo góp ý");

        Assert.Equal(TagPublishRequestStatus.Pending, request.Status);
        Assert.Equal("Đã đổi tên tag theo góp ý", request.RequestNote);
        Assert.Null(request.ReviewComment);
        Assert.Null(request.ReviewedAt);
        Assert.NotNull(request.UpdatedAt);
    }

    /// <summary>UTCID02 · B1=T · Type A — yêu cầu đang chờ duyệt, chưa bị từ chối.</summary>
    [Fact]
    public void UTCID02_PendingRequest_ThrowsInvalidOperation()
    {
        var request = BuildPending();

        var ex = Assert.Throws<InvalidOperationException>(
            () => request.Resubmit(OwnerId, "Gửi lại"));

        Assert.Equal("Chỉ yêu cầu đã bị từ chối mới được gửi lại.", ex.Message);
    }

    /// <summary>UTCID03 · B1=T · Type A — yêu cầu đã được duyệt.</summary>
    [Fact]
    public void UTCID03_ApprovedRequest_ThrowsInvalidOperation()
    {
        var request = BuildApproved();

        var ex = Assert.Throws<InvalidOperationException>(
            () => request.Resubmit(OwnerId, "Gửi lại"));

        Assert.Equal("Chỉ yêu cầu đã bị từ chối mới được gửi lại.", ex.Message);
    }

    /// <summary>
    /// UTCID04 · B1=F, B2=T · Type A — thiếu người gửi.
    /// B2 chỉ chạm được khi B1 = F, tức phải dựng trạng thái Rejected trước.
    /// </summary>
    [Fact]
    public void UTCID04_EmptyRequesterId_ThrowsArgumentException()
    {
        var request = BuildRejected();

        Assert.Throws<ArgumentException>(
            () => request.Resubmit(Guid.Empty, "Gửi lại"));
    }

    /// <summary>UTCID05 · B3=T · Type B — ghi chú null ⇒ chuẩn hoá về null.</summary>
    [Fact]
    public void UTCID05_NullNote_NormalizesToNull()
    {
        var request = BuildRejected();

        request.Resubmit(OwnerId, null);

        Assert.Equal(TagPublishRequestStatus.Pending, request.Status);
        Assert.Null(request.RequestNote);
    }

    /// <summary>UTCID06 · B3=T · Type B — ghi chú toàn khoảng trắng ⇒ chuẩn hoá về null.</summary>
    [Fact]
    public void UTCID06_WhitespaceOnlyNote_NormalizesToNull()
    {
        var request = BuildRejected();

        request.Resubmit(OwnerId, "   ");

        Assert.Null(request.RequestNote);
    }

    /// <summary>
    /// UTCID07 · B1=F, B2=F, B3=F · Type N — Expert khác gửi lại thay người tạo ban đầu,
    /// ghi chú thừa khoảng trắng ⇒ RequestedById đổi chủ và ghi chú được Trim.
    /// </summary>
    [Fact]
    public void UTCID07_DifferentExpertResubmits_UpdatesOwnerAndTrimsNote()
    {
        var request = BuildRejected();

        request.Resubmit(OtherExpertId, "  Bổ sung mô tả tag  ");

        Assert.Equal(OtherExpertId, request.RequestedById);
        Assert.Equal("Bổ sung mô tả tag", request.RequestNote);
    }
}
