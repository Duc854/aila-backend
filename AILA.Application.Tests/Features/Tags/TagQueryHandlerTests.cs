using AILA.Application.Common.Interfaces;
using AILA.Application.Common.Interfaces.Repositories;
using AILA.Application.Features.Tags.Commands;
using AILA.Application.Features.Tags.Queries;
// Thư mục là GetPendingTagRequests/ nhưng namespace bên trong là GetPendingTags — không khớp nhau.
using AILA.Application.Features.Tags.Queries.GetPendingTagDetail;
using AILA.Application.Features.Tags.Queries.GetPendingTags;
using AILA.Application.Features.Tags.Queries.GetSystemTags;
using AILA.Application.Tests.Common;
using AILA.Application.Tests.Common.Builders;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using Moq;

namespace AILA.Application.Tests.Features.Tags
{
    /// <summary>
    /// Sheet: TagService · UC-61 Custom tag / UC-62 Request verification /
    /// UC-81 Review requests / UC-84 System tags.
    /// TC-UNIT-TagService-019 → 036.
    /// Nhóm handler này KHÔNG đồng nhất kiểu trả về: query trả DTO trần hoặc null,
    /// còn hai command xoá thì báo lỗi bằng THROW chứ không phải ErrorCode.
    /// </summary>
    public class TagQueryHandlerTests
    {
        private static readonly Guid ExpertId = Guid.NewGuid();
        private static readonly Guid OtherExpertId = Guid.NewGuid();

        private readonly Mock<ITagRepository> _tags = new();
        private readonly Mock<IUserRepository> _users = new();
        private readonly Mock<IGenericRepository<TagPublishRequest>> _requests = new();
        private readonly Mock<IUnitOfWork> _uow = new();

        public TagQueryHandlerTests()
        {
            _uow.Setup(u => u.Tags).Returns(_tags.Object);
            _uow.Setup(u => u.Users).Returns(_users.Object);
            _uow.Setup(u => u.Repository<TagPublishRequest>()).Returns(_requests.Object);
        }

        private static Tag ExpertTag(string name = "My Tag", string code = "my-tag", Guid? owner = null)
            => Tag.CreateByExpert(name, code, owner ?? ExpertId);

        private static Tag ExpertTagWithRequest(TagPublishRequestStatus status, Guid? owner = null)
        {
            var ownerId = owner ?? ExpertId;
            var tag = ExpertTag(owner: ownerId);
            var request = TagPublishRequest.Create(tag.Id, ownerId, "xin duyệt");
            if (status == TagPublishRequestStatus.Approved) request.Approve();
            if (status == TagPublishRequestStatus.Rejected) request.Reject("chưa đạt");
            TestEntity.SetProperty(tag, nameof(Tag.PublishRequest), request);
            return tag;
        }

        private void VerifyNotSaved()
            => _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

        // ============================================================ UC-61 CheckTagCode

        // ------------------------------------------------------------ TC-019
        // Covers: Main Flow — mã được chuẩn hoá (lower, trim, khoảng trắng thành gạch nối)
        // TRƯỚC khi tra. Nếu bỏ bước này, "AI Ethics" và "ai-ethics" sẽ thành hai tag khác nhau.
        [Fact]
        [Trait("TC", "TC-UNIT-TagService-019")]
        [Trait("UC", "UC-61")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        public async Task CheckCode_NormalisesBeforeLookup()
        {
            _tags.Setup(r => r.CodeExistsAsync("ai-ethics", It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var handler = new CheckTagCodeQueryHandler(_uow.Object);
            var taken = await handler.Handle(new CheckTagCodeQuery("  AI Ethics  "), CancellationToken.None);

            Assert.True(taken);
            _tags.Verify(r => r.CodeExistsAsync("ai-ethics", It.IsAny<CancellationToken>()), Times.Once);
        }

        // ------------------------------------------------------------ TC-020
        // Covers: BR-01 — mã rỗng trả false mà KHÔNG hỏi repository.
        // ⚠ Ghi nhận: false ở đây nghĩa là "chưa có ai dùng", nên giao diện sẽ hiểu chuỗi rỗng
        // là mã hợp lệ. Việc chặn mã rỗng phải nằm ở CreateCustomTag, không phải ở đây.
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [Trait("TC", "TC-UNIT-TagService-020")]
        [Trait("UC", "UC-61")]
        [Trait("BR", "BR-01")]
        [Trait("Type", "Input Validation")]
        [Trait("Priority", "Medium")]
        public async Task CheckCode_Blank_FalseWithoutLookup(string? code)
        {
            var handler = new CheckTagCodeQueryHandler(_uow.Object);
            var result = await handler.Handle(new CheckTagCodeQuery(code!), CancellationToken.None);

            Assert.False(result);
            _tags.Verify(r => r.CodeExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        // ============================================================ UC-61 GetTagByCode

        // ------------------------------------------------------------ TC-021
        // Covers: Main Flow — Source suy ra từ CreatedById: null = tag hệ thống.
        [Fact]
        [Trait("TC", "TC-UNIT-TagService-021")]
        [Trait("UC", "UC-61")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        public async Task GetByCode_ReportsSource()
        {
            var custom = ExpertTag();
            _tags.Setup(r => r.GetByCodeAsync("my-tag", It.IsAny<CancellationToken>())).ReturnsAsync(custom);

            var handler = new GetTagByCodeQueryHandler(_uow.Object);
            var result = await handler.Handle(new GetTagByCodeQuery("  MY-TAG  "), CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("Custom", result!.Source);
            Assert.Equal(ExpertId, result.CreatedById);
            _tags.Verify(r => r.GetByCodeAsync("my-tag", It.IsAny<CancellationToken>()), Times.Once);
        }

        // ------------------------------------------------------------ TC-022
        // Covers: AF-01 — mã rỗng hoặc mã lạ đều trả null, không ném ngoại lệ.
        [Fact]
        [Trait("TC", "TC-UNIT-TagService-022")]
        [Trait("UC", "UC-61")]
        [Trait("Type", "Boundary & Negative")]
        [Trait("Priority", "Medium")]
        public async Task GetByCode_BlankOrUnknown_Null()
        {
            var handler = new GetTagByCodeQueryHandler(_uow.Object);

            Assert.Null(await handler.Handle(new GetTagByCodeQuery("   "), CancellationToken.None));
            _tags.Verify(r => r.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);

            _tags.Setup(r => r.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Tag?)null);
            Assert.Null(await handler.Handle(new GetTagByCodeQuery("khong-ton-tai"), CancellationToken.None));
        }

        // ============================================================ UC-61 GetPublishedTags

        // ------------------------------------------------------------ TC-023
        // Covers: BR-01 — chỉ tag đã duyệt mới vào được bộ chọn khi soạn khoá học.
        // Việc lọc nằm ở repository, nên test khẳng định handler KHÔNG tự thêm/bớt gì.
        [Fact]
        [Trait("TC", "TC-UNIT-TagService-023")]
        [Trait("UC", "UC-61")]
        [Trait("BR", "BR-01")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task GetPublished_PassesRepositoryResultThrough()
        {
            var a = ExpertTag("Tag A", "tag-a");
            var b = ExpertTag("Tag B", "tag-b");
            _tags.Setup(r => r.GetPublishedTagsAsync()).ReturnsAsync(new List<Tag> { a, b });

            var handler = new GetPublishedTagsQueryHandler(_uow.Object);
            var result = await handler.Handle(new GetPublishedTagsQuery(), CancellationToken.None);

            Assert.Equal(2, result.Count);
            Assert.Contains(result, t => t.Code == "tag-a");
            Assert.Contains(result, t => t.Code == "tag-b");
        }

        // ------------------------------------------------------------ TC-024
        [Fact]
        [Trait("TC", "TC-UNIT-TagService-024")]
        [Trait("UC", "UC-61")]
        [Trait("Type", "Boundary & Negative")]
        [Trait("Priority", "Low")]
        public async Task GetPublished_None_EmptyList()
        {
            _tags.Setup(r => r.GetPublishedTagsAsync()).ReturnsAsync(new List<Tag>());

            var handler = new GetPublishedTagsQueryHandler(_uow.Object);
            var result = await handler.Handle(new GetPublishedTagsQuery(), CancellationToken.None);

            Assert.Empty(result);
        }

        // ============================================================ UC-81 Review queue

        // ------------------------------------------------------------ TC-025
        // Covers: Main Flow — hàng chờ duyệt kèm tên người gửi, tra thêm từ bảng User.
        [Fact]
        [Trait("TC", "TC-UNIT-TagService-025")]
        [Trait("UC", "UC-81")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task GetPending_ResolvesSubmitterName()
        {
            var tag = ExpertTagWithRequest(TagPublishRequestStatus.Pending);
            _tags.Setup(r => r.GetPendingVerificationRequestsAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new List<Tag> { tag });
            _users.Setup(r => r.GetByIdAsync(ExpertId))
                  .ReturnsAsync(new UserBuilder().WithFullName("Nguyen An").Build());

            var handler = new GetPendingTagsQueryHandler(_uow.Object);
            var result = await handler.Handle(new GetPendingTagsQuery(), CancellationToken.None);

            Assert.True(result.Success);
            Assert.Single(result.Data!);
            Assert.Equal("Nguyen An", result.Data![0].SubmittedBy);
        }

        // ------------------------------------------------------------ TC-026
        // Covers: AF-01 — người gửi đã bị xoá khỏi hệ thống thì hiển thị "Unknown"
        // chứ không được ném NullReferenceException làm hỏng cả hàng chờ.
        [Fact]
        [Trait("TC", "TC-UNIT-TagService-026")]
        [Trait("UC", "UC-81")]
        [Trait("Type", "Boundary & Negative")]
        [Trait("Priority", "Medium")]
        public async Task GetPending_DeletedSubmitter_ShowsUnknown()
        {
            var tag = ExpertTagWithRequest(TagPublishRequestStatus.Pending);
            _tags.Setup(r => r.GetPendingVerificationRequestsAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new List<Tag> { tag });
            _users.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);

            var handler = new GetPendingTagsQueryHandler(_uow.Object);
            var result = await handler.Handle(new GetPendingTagsQuery(), CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal("Unknown", result.Data![0].SubmittedBy);
        }

        // ------------------------------------------------------------ TC-027
        // Covers: BR-01 — lọc theo từ khoá chạy TRONG handler (không phải ở SQL), khớp cả
        // Name lẫn Code, không phân biệt hoa thường.
        [Theory]
        [InlineData("ALPHA", 1)]
        [InlineData("beta-code", 1)]
        [InlineData("khong-khop", 0)]
        [Trait("TC", "TC-UNIT-TagService-027")]
        [Trait("UC", "UC-81")]
        [Trait("BR", "BR-01")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        public async Task GetPending_FiltersByKeyword(string keyword, int expected)
        {
            var alpha = ExpertTag("Alpha", "alpha-code");
            var beta = ExpertTag("Beta", "beta-code");
            _tags.Setup(r => r.GetPendingVerificationRequestsAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new List<Tag> { alpha, beta });
            _users.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
                  .ReturnsAsync(new UserBuilder().Build());

            var handler = new GetPendingTagsQueryHandler(_uow.Object);
            var result = await handler.Handle(new GetPendingTagsQuery(keyword), CancellationToken.None);

            Assert.Equal(expected, result.Data!.Count);
        }

        // ------------------------------------------------------------ TC-028
        [Fact]
        [Trait("TC", "TC-UNIT-TagService-028")]
        [Trait("UC", "UC-81")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task GetPendingDetail_ReturnsTag()
        {
            var tag = ExpertTagWithRequest(TagPublishRequestStatus.Pending);
            _tags.Setup(r => r.GetVerificationRequestByIdAsync(tag.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(tag);
            _users.Setup(r => r.GetByIdAsync(ExpertId))
                  .ReturnsAsync(new UserBuilder().WithFullName("Nguyen An").Build());

            var handler = new GetPendingTagDetailQueryHandler(_uow.Object);
            var result = await handler.Handle(new GetPendingTagDetailQuery(tag.Id), CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal(tag.Code, result.Data!.Code);
            Assert.False(result.Data.IsPublished);
        }

        // ------------------------------------------------------------ TC-029
        // Covers: AF-01 — yêu cầu đã được admin khác xử lý xong thì không còn tra thấy.
        [Fact]
        [Trait("TC", "TC-UNIT-TagService-029")]
        [Trait("UC", "UC-81")]
        [Trait("Type", "Boundary & Negative")]
        [Trait("Priority", "Medium")]
        public async Task GetPendingDetail_Missing_NotFound()
        {
            _tags.Setup(r => r.GetVerificationRequestByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Tag?)null);

            var handler = new GetPendingTagDetailQueryHandler(_uow.Object);
            var result = await handler.Handle(new GetPendingTagDetailQuery(Guid.NewGuid()), CancellationToken.None);

            Assert.False(result.Success);
            Assert.Equal("NOT_FOUND", result.ErrorCode);
        }

        // ============================================================ UC-84 System tags

        // ------------------------------------------------------------ TC-030
        // Covers: Main Flow — mỗi tag hệ thống kèm số lần đang được dùng, tra riêng từng tag.
        [Fact]
        [Trait("TC", "TC-UNIT-TagService-030")]
        [Trait("UC", "UC-84")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        public async Task GetSystemTags_IncludesUsageCount()
        {
            var a = Tag.CreateByAdmin("Alpha", "alpha");
            var b = Tag.CreateByAdmin("Beta", "beta");
            _tags.Setup(r => r.GetSystemTagsAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new List<Tag> { a, b });
            _tags.Setup(r => r.GetUsageCountAsync(a.Id, It.IsAny<CancellationToken>())).ReturnsAsync(5);
            _tags.Setup(r => r.GetUsageCountAsync(b.Id, It.IsAny<CancellationToken>())).ReturnsAsync(0);

            var handler = new GetSystemTagsQueryHandler(_uow.Object);
            var result = await handler.Handle(new GetSystemTagsQuery(), CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal(5, result.Data!.First(t => t.Code == "alpha").UsageCount);
            Assert.Equal(0, result.Data.First(t => t.Code == "beta").UsageCount);
        }

        // ------------------------------------------------------------ TC-031
        [Fact]
        [Trait("TC", "TC-UNIT-TagService-031")]
        [Trait("UC", "UC-84")]
        [Trait("Type", "Boundary & Negative")]
        [Trait("Priority", "Low")]
        public async Task GetSystemTags_None_EmptyList()
        {
            _tags.Setup(r => r.GetSystemTagsAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new List<Tag>());

            var handler = new GetSystemTagsQueryHandler(_uow.Object);
            var result = await handler.Handle(new GetSystemTagsQuery(), CancellationToken.None);

            Assert.True(result.Success);
            Assert.Empty(result.Data!);
            _tags.Verify(r => r.GetUsageCountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        // ============================================================ UC-61 DeleteCustomTag

        // ------------------------------------------------------------ TC-032
        // Covers: Main Flow — tag nháp, chưa gán vào khoá học nào thì xoá được.
        [Fact]
        [Trait("TC", "TC-UNIT-TagService-032")]
        [Trait("UC", "UC-61")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task DeleteCustomTag_UnusedDraft_Deleted()
        {
            var tag = ExpertTag();
            _tags.Setup(r => r.GetWithPublishRequestAsync(tag.Id, It.IsAny<CancellationToken>())).ReturnsAsync(tag);
            _tags.Setup(r => r.IsAssignedToCourseAsync(tag.Id, It.IsAny<CancellationToken>())).ReturnsAsync(false);

            var handler = new DeleteCustomTagCommandHandler(_uow.Object);
            var result = await handler.Handle(new DeleteCustomTagCommand(tag.Id, ExpertId), CancellationToken.None);

            Assert.True(result.Success);
            _tags.Verify(r => r.Delete(tag), Times.Once);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        // ------------------------------------------------------------ TC-033
        // Covers: BR-01 — handler này báo lỗi bằng THROW, không phải ErrorCode.
        [Fact]
        [Trait("TC", "TC-UNIT-TagService-033")]
        [Trait("UC", "UC-61")]
        [Trait("BR", "BR-01")]
        [Trait("Type", "Security (GBR)")]
        [Trait("Priority", "Critical")]
        public async Task DeleteCustomTag_MissingOrNotOwner_Throws()
        {
            var handler = new DeleteCustomTagCommandHandler(_uow.Object);

            _tags.Setup(r => r.GetWithPublishRequestAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Tag?)null);
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                handler.Handle(new DeleteCustomTagCommand(Guid.NewGuid(), ExpertId), CancellationToken.None));

            var foreign = ExpertTag(owner: OtherExpertId);
            _tags.Setup(r => r.GetWithPublishRequestAsync(foreign.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(foreign);
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                handler.Handle(new DeleteCustomTagCommand(foreign.Id, ExpertId), CancellationToken.None));

            _tags.Verify(r => r.Delete(It.IsAny<Tag>()), Times.Never);
            VerifyNotSaved();
        }

        // ------------------------------------------------------------ TC-034
        // Covers: BR-02 — tag đang được dùng trong khoá học mà xoá thì sẽ để lại
        // bản ghi CourseTag mồ côi.
        [Fact]
        [Trait("TC", "TC-UNIT-TagService-034")]
        [Trait("UC", "UC-61")]
        [Trait("BR", "BR-02")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task DeleteCustomTag_InUse_Throws()
        {
            var tag = ExpertTag();
            _tags.Setup(r => r.GetWithPublishRequestAsync(tag.Id, It.IsAny<CancellationToken>())).ReturnsAsync(tag);
            _tags.Setup(r => r.IsAssignedToCourseAsync(tag.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var handler = new DeleteCustomTagCommandHandler(_uow.Object);
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                handler.Handle(new DeleteCustomTagCommand(tag.Id, ExpertId), CancellationToken.None));

            _tags.Verify(r => r.Delete(It.IsAny<Tag>()), Times.Never);
            VerifyNotSaved();
        }

        // ============================================================ UC-62 Withdraw request

        // ------------------------------------------------------------ TC-035
        // Covers: Main Flow — rút lại yêu cầu đang chờ; DTO trả về báo PublishRequest = null.
        [Fact]
        [Trait("TC", "TC-UNIT-TagService-035")]
        [Trait("UC", "UC-62")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        public async Task WithdrawRequest_Pending_Removed()
        {
            var tag = ExpertTagWithRequest(TagPublishRequestStatus.Pending);
            _tags.Setup(r => r.GetWithPublishRequestAsync(tag.Id, It.IsAny<CancellationToken>())).ReturnsAsync(tag);

            var handler = new DeleteTagPublishRequestCommandHandler(_uow.Object);
            var result = await handler.Handle(
                new DeleteTagPublishRequestCommand(tag.Id, ExpertId), CancellationToken.None);

            Assert.Null(result.PublishRequest);
            _requests.Verify(r => r.Delete(It.IsAny<TagPublishRequest>()), Times.Once);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        // ------------------------------------------------------------ TC-036
        // Covers: BR-01 — chỉ rút được yêu cầu ĐANG CHỜ và phải do chính người gửi.
        // Rút một yêu cầu đã duyệt sẽ âm thầm hạ một tag đang dùng công khai xuống.
        [Fact]
        [Trait("TC", "TC-UNIT-TagService-036")]
        [Trait("UC", "UC-62")]
        [Trait("BR", "BR-01")]
        [Trait("Type", "Security (GBR)")]
        [Trait("Priority", "High")]
        public async Task WithdrawRequest_NotPendingOrNotOwner_Throws()
        {
            var handler = new DeleteTagPublishRequestCommandHandler(_uow.Object);

            // (a) tag không tồn tại
            _tags.Setup(r => r.GetWithPublishRequestAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Tag?)null);
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                handler.Handle(new DeleteTagPublishRequestCommand(Guid.NewGuid(), ExpertId), CancellationToken.None));

            // (b) tag chưa từng gửi yêu cầu nào
            var noRequest = ExpertTag();
            _tags.Setup(r => r.GetWithPublishRequestAsync(noRequest.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(noRequest);
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                handler.Handle(new DeleteTagPublishRequestCommand(noRequest.Id, ExpertId), CancellationToken.None));

            // (c) yêu cầu đã được duyệt
            var approved = ExpertTagWithRequest(TagPublishRequestStatus.Approved);
            _tags.Setup(r => r.GetWithPublishRequestAsync(approved.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(approved);
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                handler.Handle(new DeleteTagPublishRequestCommand(approved.Id, ExpertId), CancellationToken.None));

            // (d) người khác rút yêu cầu của mình
            var foreign = ExpertTagWithRequest(TagPublishRequestStatus.Pending, OtherExpertId);
            _tags.Setup(r => r.GetWithPublishRequestAsync(foreign.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(foreign);
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                handler.Handle(new DeleteTagPublishRequestCommand(foreign.Id, ExpertId), CancellationToken.None));

            _requests.Verify(r => r.Delete(It.IsAny<TagPublishRequest>()), Times.Never);
            VerifyNotSaved();
        }
    }
}
