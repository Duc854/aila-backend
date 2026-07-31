using AILA.Application.Common.Interfaces;
using AILA.Application.Common.Interfaces.Repositories;
using AILA.Application.Features.AdminBlog.Commands.CreateBlog;
using AILA.Application.Features.AdminBlog.Commands.DeleteBlog;
using AILA.Application.Features.AdminBlog.Commands.PublishBlog;
using AILA.Application.Features.AdminBlog.Commands.UnpublishBlog;
using AILA.Application.Features.AdminBlog.Commands.UpdateBlog;
using AILA.Application.Features.AdminBlog.Queries.GetAdminBlogDetail;
using AILA.Application.Features.AdminBlog.Queries.GetAdminBlogList;
using AILA.Domain.Entities;
using Moq;

namespace AILA.Application.Tests.Features.Blogs
{
    /// <summary>
    /// Sheet: BlogService · UC-94 Create / UC-95 Review / UC-96 Update / UC-97 Manage status.
    /// TC-UNIT-BlogService-001 → 019.
    /// Lưu ý ràng buộc của <see cref="BlogPost"/>: tiêu đề phải 10–255 ký tự, slug và nội dung
    /// không được rỗng — constructor ném <see cref="ArgumentException"/> chứ không trả mã lỗi.
    /// </summary>
    public class AdminBlogHandlerTests
    {
        private readonly Mock<IBlogPostRepository> _blogs = new();
        private readonly Mock<IUnitOfWork> _uow = new();

        public AdminBlogHandlerTests() => _uow.Setup(u => u.BlogPosts).Returns(_blogs.Object);

        private static BlogPost Blog(
            string title = "Artificial intelligence for everyone",
            string slug = "ai-for-all",
            string content = "Nội dung bài viết.")
            => new(title, slug, content, "https://cdn/thumb.png");

        private static BlogPost PublishedBlog()
        {
            var b = Blog();
            b.Publish();
            return b;
        }

        // ============================================================ UC-94 Create

        // ------------------------------------------------------------ TC-001
        // Covers: Main Flow — bài mới LUÔN ở trạng thái nháp; đưa lên trang chủ là UC-97.
        [Fact]
        [Trait("TC", "TC-UNIT-BlogService-001")]
        [Trait("UC", "UC-94")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task Create_NewSlug_CreatesDraft()
        {
            _blogs.Setup(r => r.ExistsSlugAsync("ai-for-all", null, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(false);

            BlogPost? added = null;
            _blogs.Setup(r => r.AddAsync(It.IsAny<BlogPost>()))
                  .Callback<BlogPost>(b => added = b)
                  .Returns(Task.CompletedTask);

            var handler = new CreateBlogCommandHandler(_uow.Object);
            var result = await handler.Handle(
                new CreateBlogCommand("Artificial intelligence for everyone", "ai-for-all", "Nội dung.", null),
                CancellationToken.None);

            Assert.True(result.Success);
            Assert.NotNull(added);
            Assert.False(added!.IsPublished);
            Assert.Null(added.PublishedAt);
            _blogs.Verify(r => r.AddAsync(It.IsAny<BlogPost>()), Times.Once);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        // ------------------------------------------------------------ TC-002
        // Covers: BR-01 — slug là url công khai, trùng sẽ che mất bài cũ.
        [Fact]
        [Trait("TC", "TC-UNIT-BlogService-002")]
        [Trait("UC", "UC-94")]
        [Trait("BR", "BR-01")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task Create_DuplicateSlug_Rejected()
        {
            _blogs.Setup(r => r.ExistsSlugAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(true);

            var handler = new CreateBlogCommandHandler(_uow.Object);
            var result = await handler.Handle(
                new CreateBlogCommand("Artificial intelligence for everyone", "ai-for-all", "Nội dung.", null),
                CancellationToken.None);

            Assert.False(result.Success);
            Assert.Equal("BLOG_SLUG_EXISTS", result.ErrorCode);
            _blogs.Verify(r => r.AddAsync(It.IsAny<BlogPost>()), Times.Never);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        // ------------------------------------------------------------ TC-003
        // Covers: BR-02 biên độ dài tiêu đề (10–255) và các trường bắt buộc.
        // Constructor BlogPost ném ArgumentException — handler KHÔNG bắt, nên ngoại lệ
        // thoát ra ngoài. Đây là điểm cần lưu ý: API sẽ trả 500 chứ không phải 400.
        [Theory]
        [InlineData("Ngắn", "slug-ok", "Nội dung.")]                       // tiêu đề 4 ký tự < 10
        [InlineData("Artificial intelligence for everyone", "", "Nội dung.")] // slug rỗng
        [InlineData("Artificial intelligence for everyone", "slug-ok", "")]   // nội dung rỗng
        [Trait("TC", "TC-UNIT-BlogService-003")]
        [Trait("UC", "UC-94")]
        [Trait("BR", "BR-02")]
        [Trait("Type", "Input Validation")]
        [Trait("Priority", "Medium")]
        public async Task Create_InvalidFields_Throws(string title, string slug, string content)
        {
            _blogs.Setup(r => r.ExistsSlugAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(false);

            var handler = new CreateBlogCommandHandler(_uow.Object);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                handler.Handle(new CreateBlogCommand(title, slug, content, null), CancellationToken.None));

            _blogs.Verify(r => r.AddAsync(It.IsAny<BlogPost>()), Times.Never);
        }

        // ------------------------------------------------------------ TC-004
        // Covers: BR-02 biên trên 255 ký tự.
        [Fact]
        [Trait("TC", "TC-UNIT-BlogService-003")]
        [Trait("UC", "UC-94")]
        [Trait("BR", "BR-02")]
        [Trait("Type", "Boundary & Negative")]
        [Trait("Priority", "Medium")]
        public async Task Create_TitleBoundary_255Ok_256Throws()
        {
            _blogs.Setup(r => r.ExistsSlugAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(false);
            _blogs.Setup(r => r.AddAsync(It.IsAny<BlogPost>())).Returns(Task.CompletedTask);

            var handler = new CreateBlogCommandHandler(_uow.Object);

            var ok = await handler.Handle(
                new CreateBlogCommand(new string('a', 255), "slug-255", "Nội dung.", null),
                CancellationToken.None);
            Assert.True(ok.Success);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                handler.Handle(new CreateBlogCommand(new string('a', 256), "slug-256", "Nội dung.", null),
                    CancellationToken.None));
        }

        // ============================================================ UC-96 Update / Delete

        // ------------------------------------------------------------ TC-005
        [Fact]
        [Trait("TC", "TC-UNIT-BlogService-004")]
        [Trait("UC", "UC-96")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task Update_ValidChange_Applied()
        {
            var blog = Blog();
            _blogs.Setup(r => r.GetByIdAsync(blog.Id)).ReturnsAsync(blog);
            _blogs.Setup(r => r.ExistsSlugAsync("new-slug", blog.Id, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(false);

            var handler = new UpdateBlogCommandHandler(_uow.Object);
            var result = await handler.Handle(
                new UpdateBlogCommand(blog.Id, "A brand new headline here", "new-slug", "Nội dung mới.", null),
                CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal("A brand new headline here", blog.Title);
            Assert.Equal("new-slug", blog.Slug);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        // ------------------------------------------------------------ TC-006
        [Fact]
        [Trait("TC", "TC-UNIT-BlogService-005")]
        [Trait("UC", "UC-96")]
        [Trait("Type", "Boundary & Negative")]
        [Trait("Priority", "Medium")]
        public async Task Update_BlogMissing_NotFound()
        {
            _blogs.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((BlogPost?)null);

            var handler = new UpdateBlogCommandHandler(_uow.Object);
            var result = await handler.Handle(
                new UpdateBlogCommand(Guid.NewGuid(), "A brand new headline here", "s", "c", null),
                CancellationToken.None);

            Assert.False(result.Success);
            Assert.Equal("BLOG_NOT_FOUND", result.ErrorCode);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        // ------------------------------------------------------------ TC-007
        // Covers: BR-01 — ExistsSlugAsync nhận excludeId để bài viết không "đụng chính nó".
        // Đây là nhánh gãy nếu ai đó bỏ tham số excludeId đi.
        [Fact]
        [Trait("TC", "TC-UNIT-BlogService-006")]
        [Trait("UC", "UC-96")]
        [Trait("BR", "BR-01")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task Update_SlugUniqueExcludingSelf()
        {
            var blog = Blog();
            _blogs.Setup(r => r.GetByIdAsync(blog.Id)).ReturnsAsync(blog);

            // (a) slug thuộc về bài KHÁC -> từ chối
            _blogs.Setup(r => r.ExistsSlugAsync("taken", blog.Id, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(true);

            var handler = new UpdateBlogCommandHandler(_uow.Object);
            var denied = await handler.Handle(
                new UpdateBlogCommand(blog.Id, "A brand new headline here", "taken", "Nội dung.", null),
                CancellationToken.None);

            Assert.False(denied.Success);
            Assert.Equal("BLOG_SLUG_EXISTS", denied.ErrorCode);

            // (b) giữ nguyên slug của chính nó -> chấp nhận
            _blogs.Setup(r => r.ExistsSlugAsync("ai-for-all", blog.Id, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(false);

            var kept = await handler.Handle(
                new UpdateBlogCommand(blog.Id, "A brand new headline here", "ai-for-all", "Nội dung.", null),
                CancellationToken.None);

            Assert.True(kept.Success);
            _blogs.Verify(r => r.ExistsSlugAsync(It.IsAny<string>(), blog.Id, It.IsAny<CancellationToken>()),
                Times.Exactly(2));
        }

        // ------------------------------------------------------------ TC-008
        [Fact]
        [Trait("TC", "TC-UNIT-BlogService-007")]
        [Trait("UC", "UC-96")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        public async Task Delete_ExistingBlog_Removed()
        {
            var blog = Blog();
            _blogs.Setup(r => r.GetByIdAsync(blog.Id)).ReturnsAsync(blog);

            var handler = new DeleteBlogCommandHandler(_uow.Object);
            var result = await handler.Handle(new DeleteBlogCommand(blog.Id), CancellationToken.None);

            Assert.True(result.Success);
            _blogs.Verify(r => r.Delete(blog), Times.Once);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        // ------------------------------------------------------------ TC-009
        // Covers: AF-01 — chặn double-click / bấm xoá hai lần.
        [Fact]
        [Trait("TC", "TC-UNIT-BlogService-008")]
        [Trait("UC", "UC-96")]
        [Trait("Type", "Boundary & Negative")]
        [Trait("Priority", "Medium")]
        public async Task Delete_BlogMissing_NotFound()
        {
            _blogs.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((BlogPost?)null);

            var handler = new DeleteBlogCommandHandler(_uow.Object);
            var result = await handler.Handle(new DeleteBlogCommand(Guid.NewGuid()), CancellationToken.None);

            Assert.False(result.Success);
            Assert.Equal("BLOG_NOT_FOUND", result.ErrorCode);
            _blogs.Verify(r => r.Delete(It.IsAny<BlogPost>()), Times.Never);
        }

        // ============================================================ UC-97 Publish / Unpublish

        // ------------------------------------------------------------ TC-010
        [Fact]
        [Trait("TC", "TC-UNIT-BlogService-009")]
        [Trait("UC", "UC-97")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task Publish_Draft_GoesLive()
        {
            var blog = Blog();
            _blogs.Setup(r => r.GetByIdAsync(blog.Id)).ReturnsAsync(blog);

            var handler = new PublishBlogCommandHandler(_uow.Object);
            var result = await handler.Handle(new PublishBlogCommand(blog.Id), CancellationToken.None);

            Assert.True(result.Success);
            Assert.True(blog.IsPublished);
            Assert.NotNull(blog.PublishedAt);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        // ------------------------------------------------------------ TC-011
        // Covers: BR-01 — publish lại KHÔNG được ghi đè PublishedAt, nếu không lịch sử
        // xuất bản bị viết lại mỗi lần admin bấm nhầm. BlogPost.Publish() có guard
        // `if (IsPublished) return;` nên hành vi này đúng.
        [Fact]
        [Trait("TC", "TC-UNIT-BlogService-010")]
        [Trait("UC", "UC-97")]
        [Trait("BR", "BR-01")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        public async Task Publish_AlreadyPublished_KeepsOriginalDate()
        {
            var blog = PublishedBlog();
            var firstPublishedAt = blog.PublishedAt;
            _blogs.Setup(r => r.GetByIdAsync(blog.Id)).ReturnsAsync(blog);

            var handler = new PublishBlogCommandHandler(_uow.Object);
            var result = await handler.Handle(new PublishBlogCommand(blog.Id), CancellationToken.None);

            Assert.True(result.Success);
            Assert.True(blog.IsPublished);
            Assert.Equal(firstPublishedAt, blog.PublishedAt);
        }

        // ------------------------------------------------------------ TC-012
        [Fact]
        [Trait("TC", "TC-UNIT-BlogService-011")]
        [Trait("UC", "UC-97")]
        [Trait("Type", "Boundary & Negative")]
        [Trait("Priority", "Medium")]
        public async Task Publish_BlogMissing_NotFound()
        {
            _blogs.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((BlogPost?)null);

            var handler = new PublishBlogCommandHandler(_uow.Object);
            var result = await handler.Handle(new PublishBlogCommand(Guid.NewGuid()), CancellationToken.None);

            Assert.False(result.Success);
            Assert.Equal("BLOG_NOT_FOUND", result.ErrorCode);
        }

        // ------------------------------------------------------------ TC-013
        // Covers: Main Flow — hạ bài. Lưu ý PublishedAt được GIỮ nguyên: bài từng công khai
        // vẫn còn dấu vết ngày xuất bản đầu tiên.
        [Fact]
        [Trait("TC", "TC-UNIT-BlogService-012")]
        [Trait("UC", "UC-97")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task Unpublish_Published_BackToDraft()
        {
            var blog = PublishedBlog();
            var publishedAt = blog.PublishedAt;
            _blogs.Setup(r => r.GetByIdAsync(blog.Id)).ReturnsAsync(blog);

            var handler = new UnpublishBlogCommandHandler(_uow.Object);
            var result = await handler.Handle(new UnpublishBlogCommand(blog.Id), CancellationToken.None);

            Assert.True(result.Success);
            Assert.False(blog.IsPublished);
            Assert.Equal(publishedAt, blog.PublishedAt);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        // ------------------------------------------------------------ TC-014
        [Fact]
        [Trait("TC", "TC-UNIT-BlogService-013")]
        [Trait("UC", "UC-97")]
        [Trait("Type", "Boundary & Negative")]
        [Trait("Priority", "Medium")]
        public async Task Unpublish_BlogMissing_NotFound()
        {
            _blogs.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((BlogPost?)null);

            var handler = new UnpublishBlogCommandHandler(_uow.Object);
            var result = await handler.Handle(new UnpublishBlogCommand(Guid.NewGuid()), CancellationToken.None);

            Assert.False(result.Success);
            Assert.Equal("BLOG_NOT_FOUND", result.ErrorCode);
        }

        // ============================================================ UC-95 Review

        // ------------------------------------------------------------ TC-015
        // TotalCount phải lấy từ repository chứ không phải Items.Count, nếu không phân trang sai.
        [Fact]
        [Trait("TC", "TC-UNIT-BlogService-014")]
        [Trait("UC", "UC-95")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task List_Paged_ReportsRepositoryTotal()
        {
            var items = new List<BlogPost> { Blog(slug: "a"), Blog(slug: "b") };
            _blogs.Setup(r => r.GetPagedAdminBlogsAsync(null, 1, 2, It.IsAny<CancellationToken>()))
                  .ReturnsAsync((items, 7));

            var handler = new GetAdminBlogListQueryHandler(_uow.Object);
            var result = await handler.Handle(new GetAdminBlogListQuery(null, 1, 2), CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal(7, result.Data!.TotalCount);
            Assert.Equal(1, result.Data.PageNumber);
            Assert.Equal(2, result.Data.PageSize);
            Assert.Equal(2, result.Data.Items.Count());
        }

        // ------------------------------------------------------------ TC-016
        // Covers: BR-01 — danh sách quản trị KHÔNG lọc IsPublished, khác hẳn danh mục công khai
        // GetBlogsWithFilterQuery (UC-06). Admin phải thấy cả bài nháp để còn sửa.
        [Fact]
        [Trait("TC", "TC-UNIT-BlogService-015")]
        [Trait("UC", "UC-95")]
        [Trait("BR", "BR-01")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task List_IncludesDrafts()
        {
            var published = PublishedBlog();
            var draft = Blog(slug: "draft-post");
            _blogs.Setup(r => r.GetPagedAdminBlogsAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>(),
                        It.IsAny<CancellationToken>()))
                  .ReturnsAsync((new List<BlogPost> { published, draft }, 2));

            var handler = new GetAdminBlogListQueryHandler(_uow.Object);
            var result = await handler.Handle(new GetAdminBlogListQuery(null), CancellationToken.None);

            Assert.Equal(2, result.Data!.Items.Count());
            Assert.Contains(result.Data.Items, i => i.Slug == "draft-post");
        }

        // ------------------------------------------------------------ TC-017
        // Từ khoá tìm kiếm phải xuống tới repository nguyên vẹn (không tự lọc ở handler).
        [Theory]
        [InlineData("ai")]
        [InlineData(null)]
        [Trait("TC", "TC-UNIT-BlogService-016")]
        [Trait("UC", "UC-95")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        public async Task List_ForwardsSearchTerm(string? search)
        {
            _blogs.Setup(r => r.GetPagedAdminBlogsAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>(),
                        It.IsAny<CancellationToken>()))
                  .ReturnsAsync((new List<BlogPost>(), 0));

            var handler = new GetAdminBlogListQueryHandler(_uow.Object);
            await handler.Handle(new GetAdminBlogListQuery(search, 2, 25), CancellationToken.None);

            _blogs.Verify(r => r.GetPagedAdminBlogsAsync(search, 2, 25, It.IsAny<CancellationToken>()), Times.Once);
        }

        // ------------------------------------------------------------ TC-018
        [Fact]
        [Trait("TC", "TC-UNIT-BlogService-018")]
        [Trait("UC", "UC-95")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task Detail_ExistingBlog_ReturnsContent()
        {
            var blog = Blog();
            _blogs.Setup(r => r.GetBlogDetailAsync(blog.Id, It.IsAny<CancellationToken>())).ReturnsAsync(blog);

            var handler = new GetAdminBlogDetailQueryHandler(_uow.Object);
            var result = await handler.Handle(new GetAdminBlogDetailQuery(blog.Id), CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal(blog.Title, result.Data!.Title);
            Assert.Equal(blog.Slug, result.Data.Slug);
        }

        // ------------------------------------------------------------ TC-019
        [Fact]
        [Trait("TC", "TC-UNIT-BlogService-019")]
        [Trait("UC", "UC-95")]
        [Trait("Type", "Boundary & Negative")]
        [Trait("Priority", "Medium")]
        public async Task Detail_BlogMissing_NotFound()
        {
            _blogs.Setup(r => r.GetBlogDetailAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync((BlogPost?)null);

            var handler = new GetAdminBlogDetailQueryHandler(_uow.Object);
            var result = await handler.Handle(new GetAdminBlogDetailQuery(Guid.NewGuid()), CancellationToken.None);

            Assert.False(result.Success);
            Assert.Equal("BLOG_NOT_FOUND", result.ErrorCode);
        }
    }
}
