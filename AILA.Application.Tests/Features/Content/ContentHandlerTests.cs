using AILA.Application.Common.Interfaces;
using AILA.Application.Common.Interfaces.Repositories;
using AILA.Application.Features.Blogs.Queries.GetBlogDetail;
using AILA.Application.Features.Blogs.Queries.GetBlogsWithFilter;
using AILA.Application.Features.Notifications.Commands;
using AILA.Application.Features.Notifications.Queries;
using AILA.Application.Features.SubscriptionPlans;
using AILA.Application.Features.SubscriptionPlans.Queries.GetActiveSubscriptionPlans;
using AILA.Application.Features.SubscriptionPlans.Queries.GetSubscriptionPlanForPurchase;
using AILA.Application.Tests.Common;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using Moq;
using Shared.Wrappers;

namespace AILA.Application.Tests.Features.Content
{
    /// <summary>
    /// Sheet: ContentService · UC-02 / 06 / 07 / 09 / 14 / 17 · TC-UNIT-ContentService-001 → 018.
    ///
    /// Sheet này gom nhiều feature rời theo góc nhìn "nội dung công khai":
    ///   searchBlogs / getBlogDetail        → Features/Blogs
    ///   getSubscriptionPlans / buyNow      → Features/SubscriptionPlans
    ///   getNotifications / markRead        → Features/Notifications
    ///   getHomepage / getRecommendations   → KHÔNG tồn tại (blocked)
    /// </summary>
    public class ContentHandlerTests
    {
        private static readonly Guid UserId = Guid.NewGuid();

        private readonly Mock<IBlogPostRepository> _blogs = new();
        private readonly Mock<INotificationRepository> _notifications = new();
        private readonly Mock<ISubscriptionPlanRepository> _plans = new();
        private readonly Mock<IUnitOfWork> _uow = new();

        public ContentHandlerTests()
        {
            _uow.Setup(u => u.BlogPosts).Returns(_blogs.Object);
            _uow.Setup(u => u.Notifications).Returns(_notifications.Object);
            _uow.Setup(u => u.SubscriptionPlans).Returns(_plans.Object);
        }

        private GetBlogsWithFilterQueryHandler BlogListHandler() => new(_uow.Object);
        private GetBlogDetailQueryHandler BlogDetailHandler() => new(_uow.Object);
        private GetNotificationListQueryHandler NotificationListHandler() => new(_uow.Object);
        private MarkNotificationReadCommandHandler MarkReadHandler() => new(_uow.Object);
        private MarkAllNotificationsReadCommandHandler MarkAllReadHandler() => new(_uow.Object);
        private GetActiveSubscriptionPlansQueryHandler ActivePlansHandler() => new(_uow.Object);
        private GetSubscriptionPlanForPurchaseQueryHandler PurchasePlanHandler() => new(_uow.Object);

        private static BlogPost PublishedBlog(string title = "Bài viết về AI Literacy", int viewCount = 0)
        {
            var blog = new BlogPost(title, "bai-viet-ve-ai-literacy", "Nội dung bài viết");
            blog.Publish();
            if (viewCount > 0)
                TestEntity.SetProperty(blog, nameof(BlogPost.ViewCount), viewCount);
            return blog;
        }

        private static SubscriptionPlan Plan(string name = "Gói Cơ Bản", int tierLevel = 1, int displayOrder = 1)
            => new(name, "Mô tả gói", 199_000m, tierLevel, 30, 10_000, 20, 2, displayOrder);

        // ============================================================ TC-004 / TC-005 / TC-006
        // Covers: BR-01/BR-02 + AF-01.
        // Phạm vi L1: lọc "chỉ blog đã publish" và tìm theo từ khoá nằm trong
        // BlogPosts.GetPagedBlogsAsync (dịch sang SQL). Mock repo thì không chạm tới được,
        // nên ở đây chỉ khẳng định handler TRUYỀN ĐÚNG tham số và giữ nguyên metadata phân trang.
        [Theory]
        [InlineData(null, 0, 10)]      // TC-004: danh sách mặc định
        [InlineData("ai", 0, 10)]      // TC-005: tìm theo từ khoá
        [InlineData("xyz123", 2, 5)]   // TC-006: không khớp + trang khác
        [Trait("TC", "TC-UNIT-ContentService-004")]
        [Trait("UC", "UC-06")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        public async Task SearchBlogs_ForwardsSearchAndPaging(
            string? search, int pageIndex, int pageSize)
        {
            _blogs.Setup(r => r.GetPagedBlogsAsync(search, pageIndex, pageSize, It.IsAny<CancellationToken>()))
                  .ReturnsAsync((new List<BlogPost>(), 0));

            var result = await BlogListHandler().Handle(
                new GetBlogsWithFilterQuery(search, new PageRequest { PageIndex = pageIndex, PageSize = pageSize }),
                CancellationToken.None);

            Assert.True(result.Success);
            Assert.Empty(result.Data!.Items);
            Assert.Equal(0, result.Data.TotalItems);
            Assert.Equal(pageIndex, result.Data.PageNumber);
            Assert.Equal(pageSize, result.Data.PageSize);
            _blogs.Verify(r => r.GetPagedBlogsAsync(
                search, pageIndex, pageSize, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        [Trait("TC", "TC-UNIT-ContentService-004")]
        [Trait("UC", "UC-06")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        public async Task SearchBlogs_MapsEveryBlogToDtoWithFixedAuthor()
        {
            var blog = PublishedBlog();
            _blogs.Setup(r => r.GetPagedBlogsAsync(
                        null, 0, 10, It.IsAny<CancellationToken>()))
                  .ReturnsAsync((new List<BlogPost> { blog }, 1));

            var result = await BlogListHandler().Handle(
                new GetBlogsWithFilterQuery(null, new PageRequest { PageIndex = 0, PageSize = 10 }),
                CancellationToken.None);

            var item = Assert.Single(result.Data!.Items);
            Assert.Equal(blog.Id, item.Id);
            Assert.Equal(blog.Title, item.Title);
            Assert.Equal(blog.Slug, item.Slug);
            Assert.Equal("Admin", item.AuthorName);   // tác giả hardcode, chưa có quan hệ tác giả thật
            Assert.Equal(1, result.Data.TotalItems);
        }

        // ============================================================ TC-007
        // Covers: BR-02 tăng lượt xem.
        // ⚠ Điểm tinh tế: DTO trả về ViewCount + 1 tính TẠI CHỖ, trong khi việc tăng thật
        // do repository làm bằng câu UPDATE riêng. Nếu UPDATE đó thất bại, người dùng vẫn
        // thấy số đã tăng — hai nguồn số liệu không đi cùng nhau.
        [Fact]
        [Trait("TC", "TC-UNIT-ContentService-007")]
        [Trait("UC", "UC-07")]
        [Trait("BR", "BR-02")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        public async Task BlogDetail_Published_ViewCountAndRelated()
        {
            var blog = PublishedBlog(viewCount: 10);
            var related = PublishedBlog("Bài viết liên quan về AI");
            _blogs.Setup(r => r.GetBlogDetailAsync(blog.Id, It.IsAny<CancellationToken>())).ReturnsAsync(blog);
            _blogs.Setup(r => r.GetRelatedBlogsAsync(blog.Id, 5, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(new List<BlogPost> { related });

            var dto = await BlogDetailHandler().Handle(
                new GetBlogDetailQuery(blog.Id), CancellationToken.None);

            Assert.NotNull(dto);
            Assert.Equal(blog.Id, dto!.Id);
            Assert.Equal(11, dto.ViewCount);          // 10 + 1
            Assert.Equal("Admin", dto.AuthorName);
            Assert.Single(dto.RelatedBlogs);
            Assert.Equal(related.Id, dto.RelatedBlogs[0].Id);
            _blogs.Verify(r => r.IncrementViewCountAsync(blog.Id, It.IsAny<CancellationToken>()), Times.Once);
        }

        // ============================================================ TC-008
        // Covers: AF-01 — blog không tồn tại HOẶC chưa publish đều trả null (controller map 404).
        // Bản nháp không được tính lượt xem, và cũng không lộ ra là nó tồn tại.
        [Fact]
        [Trait("TC", "TC-UNIT-ContentService-008")]
        [Trait("UC", "UC-07")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        public async Task BlogDetail_NotFound_NullNoViewCount()
        {
            _blogs.Setup(r => r.GetBlogDetailAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync((BlogPost?)null);

            var dto = await BlogDetailHandler().Handle(
                new GetBlogDetailQuery(Guid.NewGuid()), CancellationToken.None);

            Assert.Null(dto);
            _blogs.Verify(r => r.IncrementViewCountAsync(
                It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        [Trait("TC", "TC-UNIT-ContentService-008")]
        [Trait("UC", "UC-07")]
        [Trait("Type", "Security (GBR)")]
        [Trait("Priority", "Medium")]
        public async Task BlogDetail_Draft_HiddenNoViewCount()
        {
            var draft = new BlogPost("Bản nháp chưa công khai", "ban-nhap", "Nội dung");
            _blogs.Setup(r => r.GetBlogDetailAsync(draft.Id, It.IsAny<CancellationToken>())).ReturnsAsync(draft);

            var dto = await BlogDetailHandler().Handle(
                new GetBlogDetailQuery(draft.Id), CancellationToken.None);

            Assert.False(draft.IsPublished);
            Assert.Null(dto);
            _blogs.Verify(r => r.IncrementViewCountAsync(
                It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        // ============================================================ TC-009
        // ⚠ Notes workbook ("Không tồn tại feature Subscription/getSubscriptionPlans") đã LỖI THỜI:
        // Features/SubscriptionPlans có đủ 6 handler. getSubscriptionPlans ↔ GetActiveSubscriptionPlans.
        //
        // Covers: BR-01 — chỉ trả gói đang bán, và giữ nguyên thứ tự do tầng dữ liệu sắp.
        [Fact]
        [Trait("TC", "TC-UNIT-ContentService-009")]
        [Trait("UC", "UC-09")]
        [Trait("BR", "BR-01")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        public async Task GetPlans_ActivePlansKeepRepoOrder()
        {
            var first = Plan("Gói Cơ Bản", tierLevel: 1, displayOrder: 1);
            var second = Plan("Gói Nâng Cao", tierLevel: 2, displayOrder: 2);
            _plans.Setup(r => r.GetActivePlansOrderedAsync(It.IsAny<CancellationToken>()))
                  .ReturnsAsync(new[] { second, first });   // repo đã sắp, handler không được sắp lại

            var result = await ActivePlansHandler().Handle(
                new GetActiveSubscriptionPlansQuery(), CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal(new[] { "Gói Nâng Cao", "Gói Cơ Bản" }, result.Data!.Select(p => p.Name));
        }

        // DTO công khai KHÔNG được lộ dữ liệu quản trị (Status/TierLevel/DisplayOrder).
        [Fact]
        [Trait("TC", "TC-UNIT-ContentService-009")]
        [Trait("UC", "UC-09")]
        [Trait("Type", "Security (GBR)")]
        [Trait("Priority", "High")]
        public void PublicPlanDto_DoesNotExposeAdminFields()
        {
            var fields = typeof(AILA.Application.Features.SubscriptionPlans.Dtos.SubscriptionPlanDto)
                .GetProperties().Select(p => p.Name).ToArray();

            Assert.DoesNotContain("Status", fields);
            Assert.DoesNotContain("TierLevel", fields);
            Assert.DoesNotContain("DisplayOrder", fields);
        }

        // ============================================================ TC-010
        // Covers: AF-01 — không có gói nào đang bán là danh sách RỖNG, không phải lỗi.
        [Fact]
        [Trait("TC", "TC-UNIT-ContentService-010")]
        [Trait("UC", "UC-09")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Low")]
        public async Task GetPlans_NoActivePlan_EmptyList()
        {
            _plans.Setup(r => r.GetActivePlansOrderedAsync(It.IsAny<CancellationToken>()))
                  .ReturnsAsync(Array.Empty<SubscriptionPlan>());

            var result = await ActivePlansHandler().Handle(
                new GetActiveSubscriptionPlansQuery(), CancellationToken.None);

            Assert.True(result.Success);
            Assert.Empty(result.Data!);
            Assert.Null(result.ErrorCode);
        }

        // ============================================================ TC-011
        // Covers: bước "mua ngay" — trang mua gọi lại endpoint này để xác nhận gói CÒN bán
        // tại thời điểm bấm mua, thay vì tin dữ liệu đã render trước đó.
        [Fact]
        [Trait("TC", "TC-UNIT-ContentService-011")]
        [Trait("UC", "UC-09")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        public async Task BuyNow_ActivePlan_ReturnsPublicPlanDetail()
        {
            var plan = Plan();
            _plans.Setup(r => r.GetByIdReadOnlyAsync(plan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(plan);

            var result = await PurchasePlanHandler().Handle(
                new GetSubscriptionPlanForPurchaseQuery(plan.Id), CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal(plan.Id, result.Data!.Id);
            Assert.Equal(plan.Price, result.Data.Price);
        }

        // Gói đã ngừng bán trả NotAvailable — mã riêng, không lẫn với NotFound, để trang mua
        // phân biệt được "gói không có" và "gói vừa bị gỡ".
        [Fact]
        [Trait("TC", "TC-UNIT-ContentService-011")]
        [Trait("UC", "UC-09")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        public async Task BuyNow_InactivePlan_ReturnsNotAvailable()
        {
            var plan = Plan();
            plan.Deactivate();
            _plans.Setup(r => r.GetByIdReadOnlyAsync(plan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(plan);

            var result = await PurchasePlanHandler().Handle(
                new GetSubscriptionPlanForPurchaseQuery(plan.Id), CancellationToken.None);

            Assert.False(result.Success);
            Assert.Equal(SubscriptionPlanErrors.NotAvailable, result.ErrorCode);
            Assert.Null(result.Data);
        }

        [Fact]
        [Trait("TC", "TC-UNIT-ContentService-011")]
        [Trait("UC", "UC-09")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        public async Task BuyNow_PlanNotFound_ReturnsNotFound()
        {
            _plans.Setup(r => r.GetByIdReadOnlyAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync((SubscriptionPlan?)null);

            var result = await PurchasePlanHandler().Handle(
                new GetSubscriptionPlanForPurchaseQuery(Guid.NewGuid()), CancellationToken.None);

            Assert.Equal(SubscriptionPlanErrors.NotFound, result.ErrorCode);
        }

        // ============================================================ TC-012
        // Covers: BR-01. Sắp xếp giảm dần theo thời gian nằm ở repository — ở L1 chỉ khẳng
        // định handler map đủ field và giữ nguyên thứ tự repo trả về.
        [Fact]
        [Trait("TC", "TC-UNIT-ContentService-012")]
        [Trait("UC", "UC-14")]
        [Trait("BR", "BR-01")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Low")]
        public async Task GetNotifications_MapsFieldsKeepOrder()
        {
            var unread = new Notification(UserId, "Khoá học mới", "Có khoá học mới cho bạn",
                                          NotificationType.System, "/courses/1");
            var read = new Notification(UserId, "Đã duyệt tag", "Tag của bạn đã được duyệt",
                                        NotificationType.System);
            read.MarkAsRead();
            _notifications.Setup(r => r.GetAllByUserIdAsync(UserId))
                          .ReturnsAsync(new List<Notification> { unread, read });

            var result = await NotificationListHandler().Handle(
                new GetNotificationListQuery(UserId), CancellationToken.None);

            Assert.Equal(2, result.Count);
            Assert.Equal(unread.Id, result[0].Id);
            Assert.Equal("Khoá học mới", result[0].Title);
            Assert.Equal("Có khoá học mới cho bạn", result[0].Body);
            Assert.False(result[0].IsRead);
            Assert.Null(result[0].ReadAt);
            Assert.Equal("/courses/1", result[0].RedirectUrl);
            Assert.True(result[1].IsRead);
            Assert.NotNull(result[1].ReadAt);
        }

        // ============================================================ TC-015
        // Covers: AF-01 — chưa có thông báo nào là danh sách rỗng, không phải lỗi.
        [Fact]
        [Trait("TC", "TC-UNIT-ContentService-015")]
        [Trait("UC", "UC-14")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Low")]
        public async Task GetNotifications_None_EmptyList()
        {
            _notifications.Setup(r => r.GetAllByUserIdAsync(UserId)).ReturnsAsync(new List<Notification>());

            var result = await NotificationListHandler().Handle(
                new GetNotificationListQuery(UserId), CancellationToken.None);

            Assert.Empty(result);
        }

        // ============================================================ TC-013
        // Covers: BR-02 unread → read.
        // Handler chỉ uỷ quyền cho repository rồi lưu; việc đổi IsRead/ReadAt nằm ở repo,
        // nên L1 khẳng định đúng hai điều: gọi đúng repo với đúng userId, và có lưu.
        // userId đi kèm là rào chắn để không đọc hộ thông báo của người khác.
        [Fact]
        [Trait("TC", "TC-UNIT-ContentService-013")]
        [Trait("UC", "UC-14")]
        [Trait("BR", "BR-02")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        public async Task MarkRead_DelegatesScopedByUser()
        {
            var notificationId = Guid.NewGuid();

            var result = await MarkReadHandler().Handle(
                new MarkNotificationReadCommand(notificationId, UserId), CancellationToken.None);

            Assert.True(result.Success);
            _notifications.Verify(r => r.MarkAsReadAsync(
                notificationId, UserId, It.IsAny<CancellationToken>()), Times.Once);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        // ============================================================ TC-014
        [Fact]
        [Trait("TC", "TC-UNIT-ContentService-014")]
        [Trait("UC", "UC-14")]
        [Trait("BR", "BR-03")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Low")]
        public async Task MarkAllRead_DelegatesAndSaves()
        {
            var result = await MarkAllReadHandler().Handle(
                new MarkAllNotificationsReadCommand(UserId), CancellationToken.None);

            Assert.True(result.Success);
            _notifications.Verify(r => r.MarkAllAsReadAsync(UserId, It.IsAny<CancellationToken>()), Times.Once);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
