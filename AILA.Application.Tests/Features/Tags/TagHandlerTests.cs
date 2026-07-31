using AILA.Application.Common.Interfaces;
using AILA.Application.Common.Interfaces.Repositories;
using AILA.Application.Features.Tags.Commands;
using AILA.Application.Features.Tags.Commands.CreateSystemTag;
using AILA.Application.Features.Tags.Commands.RemoveSystemTag;
using AILA.Application.Features.Tags.Commands.ReviewTagVerifications;
using AILA.Application.Features.Tags.Commands.UpdateSystemTag;
using AILA.Application.Features.Tags.Queries;
using AILA.Application.Tests.Common;
using AILA.Application.Tests.Common.Builders;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using Moq;

namespace AILA.Application.Tests.Features.Tags
{
    /// <summary>
    /// Sheet: TagService · UC-70 / UC-71 / UC-85 → UC-88 · TC-UNIT-TagService-001 → 017.
    ///
    /// Hai nhóm handler dùng hai cơ chế báo lỗi khác nhau — test phải bám đúng từng cái:
    ///   CreateCustomTag / RequestTagVerification → trả DTO trần, báo lỗi bằng THROW
    ///   CreateSystemTag / UpdateSystemTag / RemoveSystemTag / ReviewTagVerification → ResponseDto + ErrorCode
    /// </summary>
    public class TagHandlerTests
    {
        private static readonly Guid ExpertId = Guid.NewGuid();

        private readonly Mock<ITagRepository> _tags = new();
        private readonly Mock<IExpertRepository> _experts = new();
        private readonly Mock<IGenericRepository<Tag>> _tagGeneric = new();
        private readonly Mock<IGenericRepository<TagPublishRequest>> _requests = new();
        private readonly Mock<IUnitOfWork> _uow = new();

        public TagHandlerTests()
        {
            _uow.Setup(u => u.Tags).Returns(_tags.Object);
            _uow.Setup(u => u.Experts).Returns(_experts.Object);
            _uow.Setup(u => u.Repository<Tag>()).Returns(_tagGeneric.Object);
            _uow.Setup(u => u.Repository<TagPublishRequest>()).Returns(_requests.Object);
        }

        private CreateCustomTagCommandHandler CustomTagHandler() => new(_uow.Object);
        private RequestTagVerificationCommandHandler VerificationHandler() => new(_uow.Object);
        private ReviewTagVerificationCommandHandler ReviewHandler() => new(_uow.Object);
        private CreateSystemTagCommandHandler CreateSystemHandler() => new(_uow.Object);
        private UpdateSystemTagCommandHandler UpdateSystemHandler() => new(_uow.Object);
        private RemoveSystemTagCommandHandler RemoveSystemHandler() => new(_uow.Object);

        private void VerifyNotSaved()
            => _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

        /// <summary>Tag của expert, kèm PublishRequest ở trạng thái chỉ định (null = chưa gửi).</summary>
        private static Tag ExpertTagWithRequest(TagPublishRequestStatus? requestStatus)
        {
            var tag = Tag.CreateByExpert("My Tag", "my-tag", ExpertId);

            if (requestStatus is null)
                return tag;

            var request = TagPublishRequest.Create(tag.Id, ExpertId, "xin duyệt");
            if (requestStatus == TagPublishRequestStatus.Approved) request.Approve();
            if (requestStatus == TagPublishRequestStatus.Rejected) request.Reject("chưa đạt");

            TestEntity.SetProperty(tag, nameof(Tag.PublishRequest), request);
            return tag;
        }

        // ============================================================ TC-001
        // Covers: Main Flow — tag do expert tạo LUÔN unpublished, chờ admin duyệt.
        // ⚠ Lệch UCS: BR-02/BR-03 nói "gán tag vào course", nhưng command KHÔNG có CourseId —
        // việc gán tag diễn ra ở CreateCourse/EditCourse.AssignTags.
        [Fact]
        [Trait("TC", "TC-UNIT-TagService-001")]
        [Trait("UC", "UC-70")]
        [Trait("BR", "BR-02")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task CreateCustomTag_Valid_UnpublishedOwned()
        {
            _experts.Setup(r => r.GetReadonlyWithUserAsync(ExpertId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new ExpertBuilder().Build());
            _tags.Setup(r => r.CodeExistsAsync("my-tag", It.IsAny<CancellationToken>())).ReturnsAsync(false);

            Tag? added = null;
            _tags.Setup(r => r.AddAsync(It.IsAny<Tag>()))
                 .Callback<Tag>(t => added = t)
                 .Returns(Task.CompletedTask);

            var dto = await CustomTagHandler().Handle(
                new CreateCustomTagCommand(ExpertId, "My Tag", "My Tag"), CancellationToken.None);

            Assert.NotNull(added);
            Assert.False(added!.IsPublished);            // chờ duyệt, không tự công khai
            Assert.Equal(ExpertId, added.CreatedById);
            Assert.Equal("my-tag", added.Code);          // chuẩn hoá: lower + trim + space → '-'
            Assert.False(dto.IsPublished);
            Assert.Null(dto.PublishRequest);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            // Không có đường nào gán tag vào khoá học từ đây.
            Assert.DoesNotContain(
                typeof(CreateCustomTagCommand).GetProperties(),
                p => p.Name.Contains("Course", StringComparison.OrdinalIgnoreCase));
        }

        // ============================================================ TC-002  ⚠ DEFECT
        // UCS AF-01 nói code trùng thì REUSE tag đã tồn tại. Code lại NÉM exception —
        // expert gõ trúng một tag đã có sẽ bị chặn thay vì được dùng lại nó.
        [Fact(Skip = "DEF-TAG-01 - A duplicate tag code throws instead of reusing the existing tag")]
        [Trait("TC", "TC-UNIT-TagService-002")]
        [Trait("UC", "UC-70")]
        [Trait("BR", "BR-01")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        [Trait("Defect", "DEF-TAG-01")]
        public async Task CreateCustomTag_DuplicateCode_Throws()
        {
            _experts.Setup(r => r.GetReadonlyWithUserAsync(ExpertId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new ExpertBuilder().Build());
            _tags.Setup(r => r.CodeExistsAsync("existing", It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => CustomTagHandler().Handle(
                    new CreateCustomTagCommand(ExpertId, "Existing", "Existing"), CancellationToken.None));

            Assert.Contains("đã tồn tại", ex.Message);
            _tags.Verify(r => r.AddAsync(It.IsAny<Tag>()), Times.Never);
            VerifyNotSaved();
        }

        [Fact]
        [Trait("TC", "TC-UNIT-TagService-001")]
        [Trait("UC", "UC-70")]
        [Trait("Type", "Security (GBR)")]
        [Trait("Priority", "Medium")]
        public async Task CreateCustomTag_NoExpert_ThrowsEarly()
        {
            _experts.Setup(r => r.GetReadonlyWithUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((Expert?)null);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => CustomTagHandler().Handle(
                    new CreateCustomTagCommand(ExpertId, "My Tag", "my-tag"), CancellationToken.None));

            _tags.Verify(r => r.CodeExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            VerifyNotSaved();
        }

        // ============================================================ TC-003
        // Covers: BR-04 visibility.
        // Phạm vi L1: luật "expert chỉ thấy tag published + tag chưa duyệt CỦA CHÍNH MÌNH"
        // được thực thi trong Tags.GetByExpertAsync (dịch sang SQL) — mock repo thì không
        // chạm tới được. Ở đây chỉ khẳng định handler truyền đúng ExpertId và map đủ field,
        // gồm cả trạng thái yêu cầu duyệt để UI hiện đúng nhãn. Kiểm chứng luật lọc thuộc L2.
        [Fact]
        [Trait("TC", "TC-UNIT-TagService-003")]
        [Trait("UC", "UC-70")]
        [Trait("BR", "BR-04")]
        [Trait("Type", "Security (GBR)")]
        [Trait("Priority", "Medium")]
        public async Task GetMyTags_MapsPublishRequestState()
        {
            var pending = ExpertTagWithRequest(TagPublishRequestStatus.Pending);
            var rejected = ExpertTagWithRequest(TagPublishRequestStatus.Rejected);
            var noRequest = ExpertTagWithRequest(null);
            _tags.Setup(r => r.GetByExpertAsync(ExpertId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new List<Tag> { pending, rejected, noRequest });

            var result = await new GetMyTagsQueryHandler(_uow.Object).Handle(
                new GetMyTagsQuery(ExpertId), CancellationToken.None);

            Assert.Equal(3, result.Count);
            Assert.Equal("Pending", result[0].PublishRequest?.Status);
            Assert.Equal("Rejected", result[1].PublishRequest?.Status);
            Assert.Equal("chưa đạt", result[1].PublishRequest?.ReviewComment);
            Assert.Null(result[2].PublishRequest);          // chưa gửi yêu cầu nào
            Assert.All(result, t => Assert.False(t.IsPublished));
            _tags.Verify(r => r.GetByExpertAsync(ExpertId, It.IsAny<CancellationToken>()), Times.Once);
        }

        // ============================================================ TC-004
        // Covers: Main Flow / BR-03 — gửi yêu cầu duyệt lần đầu tạo TagPublishRequest Pending.
        [Fact]
        [Trait("TC", "TC-UNIT-TagService-004")]
        [Trait("UC", "UC-71")]
        [Trait("BR", "BR-03")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        public async Task RequestVerify_FirstTime_CreatesPending()
        {
            var tag = ExpertTagWithRequest(null);
            _tags.SetupSequence(r => r.GetWithPublishRequestAsync(tag.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(tag)
                 .ReturnsAsync(ExpertTagWithRequest(TagPublishRequestStatus.Pending));

            TagPublishRequest? created = null;
            _requests.Setup(r => r.AddAsync(It.IsAny<TagPublishRequest>()))
                     .Callback<TagPublishRequest>(x => created = x)
                     .Returns(Task.CompletedTask);

            var dto = await VerificationHandler().Handle(
                new RequestTagVerificationCommand(tag.Id, ExpertId, "xin duyệt"), CancellationToken.None);

            Assert.NotNull(created);
            Assert.Equal(TagPublishRequestStatus.Pending, created!.Status);
            Assert.Equal(tag.Id, created.TagId);
            Assert.Equal("Pending", dto.PublishRequest?.Status);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        // ============================================================ TC-005
        // Covers: BR-01 — tag đã công khai thì không còn gì để xin duyệt.
        [Fact]
        [Trait("TC", "TC-UNIT-TagService-005")]
        [Trait("UC", "UC-71")]
        [Trait("BR", "BR-01")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        public async Task RequestVerify_AlreadyPublished_Throws()
        {
            var tag = Tag.CreateByAdmin("System Tag", "system-tag");   // IsPublished = true
            _tags.Setup(r => r.GetWithPublishRequestAsync(tag.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(tag);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => VerificationHandler().Handle(
                    new RequestTagVerificationCommand(tag.Id, ExpertId, null), CancellationToken.None));

            Assert.Contains("đã được xuất bản", ex.Message);
            _requests.Verify(r => r.AddAsync(It.IsAny<TagPublishRequest>()), Times.Never);
            VerifyNotSaved();
        }

        // ============================================================ TC-006
        // Covers: BR-02 — mỗi tag chỉ được có một yêu cầu chờ duyệt tại một thời điểm.
        [Fact]
        [Trait("TC", "TC-UNIT-TagService-006")]
        [Trait("UC", "UC-71")]
        [Trait("BR", "BR-02")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        public async Task RequestVerify_PendingExists_Throws()
        {
            var tag = ExpertTagWithRequest(TagPublishRequestStatus.Pending);
            _tags.Setup(r => r.GetWithPublishRequestAsync(tag.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(tag);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => VerificationHandler().Handle(
                    new RequestTagVerificationCommand(tag.Id, ExpertId, null), CancellationToken.None));

            Assert.Contains("chờ duyệt", ex.Message);
            _requests.Verify(r => r.AddAsync(It.IsAny<TagPublishRequest>()), Times.Never);
            VerifyNotSaved();
        }

        // ------------------------------------------------------------ TC-018 (MỚI)
        // Nhánh thứ ba của handler: yêu cầu đã bị TỪ CHỐI thì được gửi lại (Resubmit),
        // không tạo bản ghi mới và không bị chặn. Workbook không có TC nào phủ.
        // → cần thêm dòng TC-UNIT-TagService-018 vào sheet TagService.
        [Fact]
        [Trait("TC", "TC-UNIT-TagService-018")]
        [Trait("UC", "UC-71")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        public async Task RequestVerify_AfterReject_Resubmits()
        {
            var tag = ExpertTagWithRequest(TagPublishRequestStatus.Rejected);
            _tags.Setup(r => r.GetWithPublishRequestAsync(tag.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(tag);

            await VerificationHandler().Handle(
                new RequestTagVerificationCommand(tag.Id, ExpertId, "đã sửa"), CancellationToken.None);

            Assert.Equal(TagPublishRequestStatus.Pending, tag.PublishRequest!.Status);
            _requests.Verify(r => r.AddAsync(It.IsAny<TagPublishRequest>()), Times.Never);   // gửi lại, không tạo mới
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        // ============================================================ TC-007
        // Covers: PENDING → Approved + tag được công khai.
        [Fact]
        [Trait("TC", "TC-UNIT-TagService-007")]
        [Trait("UC", "UC-85")]
        [Trait("BR", "BR-02")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task Review_Approve_ApprovesAndPublishes()
        {
            var tag = ExpertTagWithRequest(TagPublishRequestStatus.Pending);
            _tags.Setup(r => r.GetVerificationRequestByIdAsync(tag.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(tag);

            var result = await ReviewHandler().Handle(
                new ReviewTagVerificationCommand(tag.Id, TagPublishRequestStatus.Approved),
                CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal(TagPublishRequestStatus.Approved, tag.PublishRequest!.Status);
            Assert.True(tag.IsPublished);
            Assert.NotNull(tag.PublishRequest.ReviewedAt);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        // ============================================================ TC-008
        // Covers: BR-03 — từ chối kèm lý do; tag KHÔNG được công khai.
        // ⚠ Không có bước gửi notification cho expert như UCS mô tả.
        [Fact(Skip = "DEF-TAG-02 - Rejecting a tag does not notify the expert")]
        [Trait("TC", "TC-UNIT-TagService-008")]
        [Trait("UC", "UC-85")]
        [Trait("BR", "BR-03")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        [Trait("Defect", "DEF-TAG-02")]
        public async Task Review_RejectWithNote_StoresReason()
        {
            var tag = ExpertTagWithRequest(TagPublishRequestStatus.Pending);
            _tags.Setup(r => r.GetVerificationRequestByIdAsync(tag.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(tag);

            var result = await ReviewHandler().Handle(
                new ReviewTagVerificationCommand(tag.Id, TagPublishRequestStatus.Rejected, "không đạt"),
                CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal(TagPublishRequestStatus.Rejected, tag.PublishRequest!.Status);
            Assert.Equal("không đạt", tag.PublishRequest.ReviewComment);
            Assert.False(tag.IsPublished);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        // ============================================================ TC-009
        // Covers: BR-03 — từ chối bắt buộc có lý do. Handler chặn TRƯỚC khi gọi domain,
        // nếu không TagPublishRequest.Reject sẽ ném ArgumentException → lỗi 500.
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [Trait("TC", "TC-UNIT-TagService-009")]
        [Trait("UC", "UC-85")]
        [Trait("BR", "BR-03")]
        [Trait("Type", "Input Validation")]
        [Trait("Priority", "Medium")]
        public async Task Review_RejectNoNote_MissingReason(string? note)
        {
            var tag = ExpertTagWithRequest(TagPublishRequestStatus.Pending);
            _tags.Setup(r => r.GetVerificationRequestByIdAsync(tag.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(tag);

            var result = await ReviewHandler().Handle(
                new ReviewTagVerificationCommand(tag.Id, TagPublishRequestStatus.Rejected, note),
                CancellationToken.None);

            Assert.False(result.Success);
            Assert.Equal("MISSING_REJECTION_REASON", result.ErrorCode);
            Assert.Equal(TagPublishRequestStatus.Pending, tag.PublishRequest!.Status);   // giữ nguyên
            VerifyNotSaved();
        }

        // ============================================================ TC-010
        // Covers: BR-01 — chỉ yêu cầu đang Pending mới review được.
        [Theory]
        [InlineData(TagPublishRequestStatus.Approved)]
        [InlineData(TagPublishRequestStatus.Rejected)]
        [Trait("TC", "TC-UNIT-TagService-010")]
        [Trait("UC", "UC-85")]
        [Trait("BR", "BR-01")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        public async Task Review_AlreadyReviewed_InvalidStatus(TagPublishRequestStatus status)
        {
            var tag = ExpertTagWithRequest(status);
            _tags.Setup(r => r.GetVerificationRequestByIdAsync(tag.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(tag);

            var result = await ReviewHandler().Handle(
                new ReviewTagVerificationCommand(tag.Id, TagPublishRequestStatus.Approved),
                CancellationToken.None);

            Assert.Equal("INVALID_STATUS", result.ErrorCode);
            VerifyNotSaved();
        }

        [Fact]
        [Trait("TC", "TC-UNIT-TagService-010")]
        [Trait("UC", "UC-85")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        public async Task Review_NoRequest_RequestNotFound()
        {
            var tag = ExpertTagWithRequest(null);
            _tags.Setup(r => r.GetVerificationRequestByIdAsync(tag.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(tag);

            var result = await ReviewHandler().Handle(
                new ReviewTagVerificationCommand(tag.Id, TagPublishRequestStatus.Approved),
                CancellationToken.None);

            Assert.Equal("REQUEST_NOT_FOUND", result.ErrorCode);
            VerifyNotSaved();
        }

        [Fact]
        [Trait("TC", "TC-UNIT-TagService-007")]
        [Trait("UC", "UC-85")]
        [Trait("Type", "Input Validation")]
        [Trait("Priority", "Low")]
        public async Task Review_EmptyTagId_RejectedNoDb()
        {
            var result = await ReviewHandler().Handle(
                new ReviewTagVerificationCommand(Guid.Empty, TagPublishRequestStatus.Approved),
                CancellationToken.None);

            Assert.Equal("INVALID_TAG_ID", result.ErrorCode);
            _tags.Verify(r => r.GetVerificationRequestByIdAsync(
                It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        // ============================================================ TC-011
        // Covers: BR-02 — system tag do admin tạo được công khai NGAY, không cần duyệt.
        [Fact]
        [Trait("TC", "TC-UNIT-TagService-011")]
        [Trait("UC", "UC-86")]
        [Trait("BR", "BR-02")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task CreateSystemTag_Valid_PublishedSlug()
        {
            _tags.Setup(r => r.GetByCodeAsync("ai-ethics", It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Tag?)null);

            Tag? added = null;
            _tags.Setup(r => r.AddAsync(It.IsAny<Tag>()))
                 .Callback<Tag>(t => added = t)
                 .Returns(Task.CompletedTask);

            var result = await CreateSystemHandler().Handle(
                new CreateSystemTagCommand("AI Ethics"), CancellationToken.None);

            Assert.True(result.Success);
            Assert.NotNull(added);
            Assert.True(added!.IsPublished);      // khác custom tag: công khai luôn
            Assert.Null(added.CreatedById);       // null = system tag
            Assert.Equal("ai-ethics", added.Code);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        // ============================================================ TC-012
        [Fact]
        [Trait("TC", "TC-UNIT-TagService-012")]
        [Trait("UC", "UC-86")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        public async Task CreateSystemTag_CodeUsed_DuplicateTag()
        {
            _tags.Setup(r => r.GetByCodeAsync("ai-ethics", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Tag.CreateByAdmin("AI Ethics", "ai-ethics"));

            var result = await CreateSystemHandler().Handle(
                new CreateSystemTagCommand("AI Ethics"), CancellationToken.None);

            Assert.False(result.Success);
            Assert.Equal("DUPLICATE_TAG", result.ErrorCode);
            _tags.Verify(r => r.AddAsync(It.IsAny<Tag>()), Times.Never);
        }

        // ⚠ DEFECT: nếu code trùng với một CUSTOM tag (CreatedById != null), handler không
        // báo trùng mà gọi Update rồi trả về DTO với Source = "System" — trong khi tag đó vẫn
        // là custom tag chưa duyệt (CreatedById != null, IsPublished có thể = false).
        // Admin tưởng đã tạo được system tag, thực tế không có gì được nâng cấp.
        [Fact(Skip = "DEF-TAG-03 - Creating a system tag does not promote a colliding custom tag")]
        [Trait("TC", "TC-UNIT-TagService-012")]
        [Trait("UC", "UC-86")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        [Trait("Defect", "DEF-TAG-03")]
        public async Task CreateSystemTag_CustomCollision_Bug()
        {
            var customTag = Tag.CreateByExpert("AI Ethics", "ai-ethics", ExpertId);
            _tags.Setup(r => r.GetByCodeAsync("ai-ethics", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(customTag);

            var result = await CreateSystemHandler().Handle(
                new CreateSystemTagCommand("AI Ethics"), CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal("System", result.Data!.Source);      // DTO nói là System...
            Assert.Equal(ExpertId, customTag.CreatedById);    // ...nhưng vẫn là custom tag
            Assert.False(customTag.IsPublished);              // và vẫn chưa được công khai
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("a")]                       // 1 ký tự — biên dưới - 1
        [Trait("TC", "TC-UNIT-TagService-012")]
        [Trait("UC", "UC-86")]
        [Trait("Type", "Boundary & Negative")]
        [Trait("Priority", "Medium")]
        public async Task CreateSystemTag_NameOutOfRange_Invalid(string name)
        {
            var result = await CreateSystemHandler().Handle(
                new CreateSystemTagCommand(name), CancellationToken.None);

            Assert.Equal("INVALID_NAME", result.ErrorCode);
            _tags.Verify(r => r.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            VerifyNotSaved();
        }

        // ============================================================ TC-013
        [Fact]
        [Trait("TC", "TC-UNIT-TagService-013")]
        [Trait("UC", "UC-87")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        public async Task UpdateSystemTag_Valid_UpdatesNameCode()
        {
            var tag = Tag.CreateByAdmin("AI Ethics", "ai-ethics");
            _tags.Setup(r => r.GetByIdAsync(tag.Id)).ReturnsAsync(tag);
            _tags.Setup(r => r.CodeExistsAsync("ai-safety", It.IsAny<CancellationToken>())).ReturnsAsync(false);
            _tags.Setup(r => r.IsAssignedToCourseAsync(tag.Id, It.IsAny<CancellationToken>())).ReturnsAsync(false);

            var result = await UpdateSystemHandler().Handle(
                new UpdateSystemTagCommand(tag.Id, "AI Safety", "ai-safety"), CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal("AI Safety", tag.Name);
            Assert.Equal("ai-safety", tag.Code);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        // Custom tag không sửa được qua UC-87.
        [Fact]
        [Trait("TC", "TC-UNIT-TagService-013")]
        [Trait("UC", "UC-87")]
        [Trait("Type", "Security (GBR)")]
        [Trait("Priority", "Medium")]
        public async Task UpdateSystemTag_CustomTag_NotUpdatable()
        {
            var tag = Tag.CreateByExpert("My Tag", "my-tag", ExpertId);
            _tags.Setup(r => r.GetByIdAsync(tag.Id)).ReturnsAsync(tag);

            var result = await UpdateSystemHandler().Handle(
                new UpdateSystemTagCommand(tag.Id, "Đổi tên", "doi-ten"), CancellationToken.None);

            Assert.Equal("CUSTOM_TAG_NOT_UPDATABLE", result.ErrorCode);
            Assert.Equal("My Tag", tag.Name);
            VerifyNotSaved();
        }

        // ============================================================ TC-014
        [Fact]
        [Trait("TC", "TC-UNIT-TagService-014")]
        [Trait("UC", "UC-87")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        public async Task UpdateSystemTag_CodeTaken_Duplicate()
        {
            var tag = Tag.CreateByAdmin("Old", "old");
            _tags.Setup(r => r.GetByIdAsync(tag.Id)).ReturnsAsync(tag);
            _tags.Setup(r => r.CodeExistsAsync("existing", It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var result = await UpdateSystemHandler().Handle(
                new UpdateSystemTagCommand(tag.Id, "Existing", "existing"), CancellationToken.None);

            Assert.Equal("DUPLICATE_TAG", result.ErrorCode);
            Assert.Equal("old", tag.Code);
            VerifyNotSaved();
        }

        // Giữ nguyên code của chính nó thì KHÔNG bị coi là trùng — nếu không, đổi mỗi tên
        // mà giữ nguyên slug sẽ bị chặn oan.
        [Fact]
        [Trait("TC", "TC-UNIT-TagService-014")]
        [Trait("UC", "UC-87")]
        [Trait("Type", "Boundary & Negative")]
        [Trait("Priority", "Medium")]
        public async Task UpdateSystemTag_CodeUnchanged_Ok()
        {
            var tag = Tag.CreateByAdmin("AI Ethics", "ai-ethics");
            _tags.Setup(r => r.GetByIdAsync(tag.Id)).ReturnsAsync(tag);
            _tags.Setup(r => r.CodeExistsAsync("ai-ethics", It.IsAny<CancellationToken>())).ReturnsAsync(true);
            _tags.Setup(r => r.IsAssignedToCourseAsync(tag.Id, It.IsAny<CancellationToken>())).ReturnsAsync(false);

            var result = await UpdateSystemHandler().Handle(
                new UpdateSystemTagCommand(tag.Id, "AI Ethics", "ai-ethics"), CancellationToken.None);

            Assert.True(result.Success);
        }

        // ============================================================ TC-015
        // Covers: BR-02 — tag đang được khoá học dùng thì không sửa được.
        [Fact]
        [Trait("TC", "TC-UNIT-TagService-015")]
        [Trait("UC", "UC-87")]
        [Trait("BR", "BR-02")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task UpdateSystemTag_InUse_Rejected()
        {
            var tag = Tag.CreateByAdmin("AI Ethics", "ai-ethics");
            _tags.Setup(r => r.GetByIdAsync(tag.Id)).ReturnsAsync(tag);
            _tags.Setup(r => r.CodeExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
            _tags.Setup(r => r.IsAssignedToCourseAsync(tag.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var result = await UpdateSystemHandler().Handle(
                new UpdateSystemTagCommand(tag.Id, "AI Safety", "ai-safety"), CancellationToken.None);

            Assert.Equal("TAG_IN_USE", result.ErrorCode);
            Assert.Equal("AI Ethics", tag.Name);
            VerifyNotSaved();
        }

        // ============================================================ TC-016
        [Fact]
        [Trait("TC", "TC-UNIT-TagService-016")]
        [Trait("UC", "UC-88")]
        [Trait("BR", "BR-01")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        public async Task RemoveSystemTag_Unused_Deletes()
        {
            var tag = Tag.CreateByAdmin("AI Ethics", "ai-ethics");
            _tagGeneric.Setup(r => r.GetByIdAsync(tag.Id)).ReturnsAsync(tag);
            _tags.Setup(r => r.IsAssignedToCourseAsync(tag.Id, It.IsAny<CancellationToken>())).ReturnsAsync(false);

            var result = await RemoveSystemHandler().Handle(
                new RemoveSystemTagCommand(tag.Id), CancellationToken.None);

            Assert.True(result.Success);
            _tagGeneric.Verify(r => r.Delete(tag), Times.Once);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        // ============================================================ TC-017
        // Covers: BR-01 — tag đang được dùng thì không xoá được, tránh khoá học mất tag.
        [Fact]
        [Trait("TC", "TC-UNIT-TagService-017")]
        [Trait("UC", "UC-88")]
        [Trait("BR", "BR-01")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task RemoveSystemTag_TagInUseByCourse_IsRejected()
        {
            var tag = Tag.CreateByAdmin("AI Ethics", "ai-ethics");
            _tagGeneric.Setup(r => r.GetByIdAsync(tag.Id)).ReturnsAsync(tag);
            _tags.Setup(r => r.IsAssignedToCourseAsync(tag.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var result = await RemoveSystemHandler().Handle(
                new RemoveSystemTagCommand(tag.Id), CancellationToken.None);

            Assert.Equal("TAG_IN_USE", result.ErrorCode);
            _tagGeneric.Verify(r => r.Delete(It.IsAny<Tag>()), Times.Never);
            VerifyNotSaved();
        }

        // Custom tag không xoá được qua UC-88 (chỉ dành cho system tag).
        [Fact]
        [Trait("TC", "TC-UNIT-TagService-016")]
        [Trait("UC", "UC-88")]
        [Trait("Type", "Security (GBR)")]
        [Trait("Priority", "Medium")]
        public async Task RemoveSystemTag_CustomTag_Rejected()
        {
            var tag = Tag.CreateByExpert("My Tag", "my-tag", ExpertId);
            _tagGeneric.Setup(r => r.GetByIdAsync(tag.Id)).ReturnsAsync(tag);

            var result = await RemoveSystemHandler().Handle(
                new RemoveSystemTagCommand(tag.Id), CancellationToken.None);

            Assert.Equal("NOT_SYSTEM_TAG", result.ErrorCode);
            _tagGeneric.Verify(r => r.Delete(It.IsAny<Tag>()), Times.Never);
            _tags.Verify(r => r.IsAssignedToCourseAsync(
                It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
            VerifyNotSaved();
        }
    }
}
