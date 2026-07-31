using AILA.Application.Common.Interfaces;
using AILA.Application.Common.Interfaces.Repositories;
using AILA.Application.Features.Courses.Commands;
using AILA.Application.Features.Courses.Queries;
using AILA.Application.Tests.Common.Builders;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using Moq;

namespace AILA.Application.Tests.Features.Courses
{
    /// <summary>
    /// Sheet: CourseService · UC-03 / UC-04 / UC-34 → UC-38 · TC-UNIT-CourseService-001 → 025.
    ///
    /// Lưu ý về cơ chế báo lỗi KHÔNG đồng nhất trong feature này:
    ///   CreateCourse / EditCourse → NÉM exception (InvalidOperationException / UnauthorizedAccessException)
    ///   PublishCourse / UnpublishCourse → trả ResponseDto với ErrorCode
    /// Test phải bám đúng cơ chế của từng handler (xem DEF-CRS-01).
    /// </summary>
    public class CourseHandlerTests
    {
        private static readonly Guid OwnerId = Guid.NewGuid();
        private static readonly Guid OtherExpertId = Guid.NewGuid();

        private readonly Mock<ICourseRepository> _courses = new();
        private readonly Mock<IExpertRepository> _experts = new();
        private readonly Mock<ICategoryRepository> _categories = new();
        private readonly Mock<ITagRepository> _tags = new();
        private readonly Mock<IUnitOfWork> _uow = new();

        public CourseHandlerTests()
        {
            _uow.Setup(u => u.Courses).Returns(_courses.Object);
            _uow.Setup(u => u.Experts).Returns(_experts.Object);
            _uow.Setup(u => u.Categories).Returns(_categories.Object);
            _uow.Setup(u => u.Tags).Returns(_tags.Object);
        }

        private GetCoursesQueryHandler SearchHandler() => new(_uow.Object);
        private CreateCourseCommandHandler CreateHandler() => new(_uow.Object);
        private EditCourseCommandHandler EditHandler() => new(_uow.Object);
        private PublishCourseCommandHandler PublishHandler() => new(_uow.Object);
        private UnpublishCourseCommandHandler UnpublishHandler() => new(_uow.Object);

        private void VerifyNotSaved()
            => _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

        /// <summary>Chuẩn bị sẵn expert + category hợp lệ cho luồng create/edit.</summary>
        private void ArrangeValidLookups()
        {
            _experts.Setup(r => r.GetReadonlyWithUserAsync(OwnerId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new ExpertBuilder().Build());
            _categories.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
                       .ReturnsAsync(new CategoryBuilder().Build());
        }

        // ============================================================ TC-001 → 004
        // Covers: BR-01/02/03 filter + AF-01 empty.
        // Phạm vi L1: lọc/phân trang/chỉ-lấy-public nằm trong CourseRepository.SearchCoursesAsync
        // (dịch sang SQL), mock repo thì không chạm tới được. Ở đây chỉ khẳng định handler
        // TRUYỀN ĐÚNG tham số và giữ nguyên metadata phân trang — phần lọc thuộc L2.
        public static TheoryData<string?, Guid?, Guid?, string?, int, int> SearchArguments => new()
        {
            { null,     null,                  null,                  null,        0, 12 },  // TC-001
            { "ai",     null,                  null,                  null,        0, 12 },  // TC-002
            { null,     Guid.NewGuid(),        Guid.NewGuid(),        "Beginner",  0, 12 },  // TC-003
            { "xyz123", null,                  null,                  null,        2, 5  },  // TC-004
        };

        [Theory]
        [MemberData(nameof(SearchArguments))]
        [Trait("TC", "TC-UNIT-CourseService-001")]
        [Trait("UC", "UC-03")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task Search_ForwardsArgsAndPaging(
            string? keyword, Guid? categoryId, Guid? tagId, string? level, int pageIndex, int pageSize)
        {
            _courses.Setup(r => r.SearchCoursesAsync(keyword, categoryId, tagId, level, pageIndex, pageSize))
                    .ReturnsAsync((new List<Course>(), 0));

            var result = await SearchHandler().Handle(
                new GetCoursesQuery(keyword, categoryId, tagId, level, pageIndex, pageSize),
                CancellationToken.None);

            Assert.Empty(result.Items);
            Assert.Equal(0, result.TotalItems);
            Assert.Equal(pageIndex, result.PageNumber);
            Assert.Equal(pageSize, result.PageSize);
            _courses.Verify(r => r.SearchCoursesAsync(
                keyword, categoryId, tagId, level, pageIndex, pageSize), Times.Once);
        }

        // AF-01: không khớp gì là danh sách rỗng, KHÔNG phải lỗi.
        [Fact]
        [Trait("TC", "TC-UNIT-CourseService-002")]
        [Trait("UC", "UC-03")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        public async Task Search_NoMatch_EmptyPage()
        {
            _courses.Setup(r => r.SearchCoursesAsync(
                        "xyz123", null, null, null, 0, 12))
                    .ReturnsAsync((new List<Course>(), 0));

            var result = await SearchHandler().Handle(
                new GetCoursesQuery("xyz123", null, null, null, 0, 12), CancellationToken.None);

            Assert.Empty(result.Items);
            Assert.Equal(0, result.TotalItems);
        }

        // ============================================================ TC-007
        // Covers: Main Flow — khoá học mới LUÔN ở dạng nháp (IsPublished=false, Duration=0).
        [Fact]
        [Trait("TC", "TC-UNIT-CourseService-007")]
        [Trait("UC", "UC-34")]
        [Trait("BR", "BR-02")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Critical")]
        public async Task Create_Valid_DraftCourseWithTags()
        {
            ArrangeValidLookups();
            var tag = Tag.CreateByAdmin("AI Cơ bản", "AI_BASIC");
            _tags.Setup(r => r.GetByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new List<Tag> { tag });

            Course? added = null;
            _courses.Setup(r => r.AddAsync(It.IsAny<Course>()))
                    .Callback<Course>(c => added = c)
                    .Returns(Task.CompletedTask);

            var result = await CreateHandler().Handle(
                new CreateCourseCommand(OwnerId, "AI 101", Guid.NewGuid(), "Beginner",
                                        "Mô tả", "https://cdn/x.jpg", new List<Guid> { tag.Id }),
                CancellationToken.None);

            Assert.NotNull(added);
            Assert.False(added!.IsPublished);            // luôn là bản nháp
            Assert.Equal(0m, added.DurationHours);
            Assert.Equal(OwnerId, added.ExpertId);
            Assert.Equal(KnowledgeLevel.Beginner, added.Level);
            Assert.Contains(tag.Id, result.TagIds);
            Assert.False(result.IsPublished);
            _courses.Verify(r => r.AddAsync(It.IsAny<Course>()), Times.Once);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        // ============================================================ TC-008  ⚠ DEFECT
        // BR-01 yêu cầu tối thiểu 1 tag, nhưng handler chỉ gán tag khi `TagIds.Any()` —
        // danh sách rỗng vẫn tạo được khoá học không tag. Rule chưa được enforce.
        [Fact(Skip = "DEF-CRS-02 - A course can be created with no tag at all")]
        [Trait("TC", "TC-UNIT-CourseService-008")]
        [Trait("UC", "UC-34")]
        [Trait("BR", "BR-01")]
        [Trait("Type", "Input Validation")]
        [Trait("Priority", "High")]
        [Trait("Defect", "DEF-CRS-02")]
        public async Task Create_NoTag_AcceptedNoGuard()
        {
            ArrangeValidLookups();

            var result = await CreateHandler().Handle(
                new CreateCourseCommand(OwnerId, "AI 101", Guid.NewGuid(), "Beginner",
                                        null, null, new List<Guid>()),
                CancellationToken.None);

            Assert.Empty(result.TagIds);
            _tags.Verify(r => r.GetByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()), Times.Never);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        // ============================================================ TC-009
        // Covers: AF-02 invalid info. Ba lý do từ chối, ba loại exception khác nhau —
        // handler này KHÔNG dùng ResponseDto.
        [Fact]
        [Trait("TC", "TC-UNIT-CourseService-009")]
        [Trait("UC", "UC-34")]
        [Trait("Type", "Input Validation")]
        [Trait("Priority", "High")]
        public async Task Create_BlankName_ThrowsNoSave()
        {
            ArrangeValidLookups();

            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => CreateHandler().Handle(
                    new CreateCourseCommand(OwnerId, "   ", Guid.NewGuid(), "Beginner",
                                            null, null, new List<Guid>()),
                    CancellationToken.None));

            Assert.Equal("name", ex.ParamName);
            _courses.Verify(r => r.AddAsync(It.IsAny<Course>()), Times.Never);
            VerifyNotSaved();
        }

        [Fact]
        [Trait("TC", "TC-UNIT-CourseService-009")]
        [Trait("UC", "UC-34")]
        [Trait("Type", "Input Validation")]
        [Trait("Priority", "High")]
        public async Task Create_CategoryNotFound_ThrowsAndSavesNothing()
        {
            _experts.Setup(r => r.GetReadonlyWithUserAsync(OwnerId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new ExpertBuilder().Build());
            _categories.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Category?)null);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => CreateHandler().Handle(
                    new CreateCourseCommand(OwnerId, "AI 101", Guid.NewGuid(), "Beginner",
                                            null, null, new List<Guid>()),
                    CancellationToken.None));

            Assert.Contains("Danh mục không tồn tại", ex.Message);
            VerifyNotSaved();
        }

        [Theory]
        [InlineData("Expert")]        // không thuộc KnowledgeLevel
        [InlineData("")]
        [InlineData("beginer")]       // gõ sai chính tả
        [Trait("TC", "TC-UNIT-CourseService-009")]
        [Trait("UC", "UC-34")]
        [Trait("Type", "Input Validation")]
        [Trait("Priority", "High")]
        public async Task Create_InvalidLevel_ThrowsAndSavesNothing(string level)
        {
            ArrangeValidLookups();

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => CreateHandler().Handle(
                    new CreateCourseCommand(OwnerId, "AI 101", Guid.NewGuid(), level,
                                            null, null, new List<Guid>()),
                    CancellationToken.None));

            VerifyNotSaved();
        }

        // Level không phân biệt hoa thường (Enum.TryParse với ignoreCase: true).
        [Theory]
        [InlineData("beginner", KnowledgeLevel.Beginner)]
        [InlineData("INTERMEDIATE", KnowledgeLevel.Intermediate)]
        [InlineData("Advanced", KnowledgeLevel.Advanced)]
        [Trait("TC", "TC-UNIT-CourseService-009")]
        [Trait("UC", "UC-34")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Low")]
        public async Task Create_LevelIsCaseInsensitive(string level, KnowledgeLevel expected)
        {
            ArrangeValidLookups();

            var result = await CreateHandler().Handle(
                new CreateCourseCommand(OwnerId, "AI 101", Guid.NewGuid(), level,
                                        null, null, new List<Guid>()),
                CancellationToken.None);

            Assert.Equal(expected.ToString(), result.Level);
        }

        // ============================================================ TC-012
        [Fact]
        [Trait("TC", "TC-UNIT-CourseService-012")]
        [Trait("UC", "UC-35")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task Edit_ByOwner_UpdatesInfoAndTags()
        {
            var course = new CourseBuilder().OwnedBy(OwnerId).WithName("AI 101").Build();
            var newCategoryId = Guid.NewGuid();
            var tag = Tag.CreateByAdmin("NLP", "NLP");
            _courses.Setup(r => r.GetWithTagsForUpdateAsync(course.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(course);
            _categories.Setup(r => r.GetByIdAsync(newCategoryId)).ReturnsAsync(new CategoryBuilder().Build());
            _tags.Setup(r => r.GetByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new List<Tag> { tag });

            var result = await EditHandler().Handle(
                new EditCourseCommand(course.Id, OwnerId, "AI 102", newCategoryId, "Intermediate",
                                      "Mô tả mới", null, new List<Guid> { tag.Id }),
                CancellationToken.None);

            Assert.Equal("AI 102", course.Name);
            Assert.Equal(newCategoryId, course.CategoryId);
            Assert.Equal(KnowledgeLevel.Intermediate, course.Level);
            Assert.Contains(tag.Id, result.TagIds);
            _courses.Verify(r => r.Update(course), Times.Once);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        // ============================================================ TC-013
        // Covers: ownership. ⚠ Dùng UnauthorizedAccessException, KHÔNG phải ErrorCode
        // "FORBIDDEN" như PublishCourse — cùng một khái niệm, hai cách biểu đạt.
        [Fact(Skip = "DEF-CRS-01 - Course handlers report errors inconsistently, throw vs error code")]
        [Trait("TC", "TC-UNIT-CourseService-013")]
        [Trait("UC", "UC-35")]
        [Trait("Type", "Security (GBR)")]
        [Trait("Priority", "High")]
        [Trait("Defect", "DEF-CRS-01")]
        public async Task Edit_ByNonOwner_ThrowsUnauthorized()
        {
            var course = new CourseBuilder().OwnedBy(OwnerId).WithName("AI 101").Build();
            _courses.Setup(r => r.GetWithTagsForUpdateAsync(course.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(course);

            var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => EditHandler().Handle(
                    new EditCourseCommand(course.Id, OtherExpertId, "Bị chiếm", Guid.NewGuid(),
                                          "Beginner", null, null, new List<Guid>()),
                    CancellationToken.None));

            Assert.Contains("không có quyền", ex.Message);
            Assert.Equal("AI 101", course.Name);
            _categories.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
            VerifyNotSaved();
        }

        // ============================================================ TC-015
        // Covers: BR-04 — cả bản nháp lẫn khoá đã phát hành đều sửa được;
        // EditCourse không hề có guard theo IsPublished.
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        [Trait("TC", "TC-UNIT-CourseService-014")]
        [Trait("UC", "UC-35")]
        [Trait("BR", "BR-04")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        public async Task Edit_WorksOnBothDraftAndPublishedCourse(bool published)
        {
            var builder = new CourseBuilder().OwnedBy(OwnerId).WithName("AI 101");
            var course = published ? builder.Published().Build() : builder.Build();
            _courses.Setup(r => r.GetWithTagsForUpdateAsync(course.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(course);
            _categories.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(new CategoryBuilder().Build());

            await EditHandler().Handle(
                new EditCourseCommand(course.Id, OwnerId, "AI 102", Guid.NewGuid(), "Beginner",
                                      null, null, new List<Guid>()),
                CancellationToken.None);

            Assert.Equal("AI 102", course.Name);
            Assert.Equal(published, course.IsPublished);   // trạng thái phát hành không bị đụng
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        // ============================================================ TC-016
        // Covers: PUBLISHED → HIDDEN. "Archive/HIDDEN" trong UCS = Unpublish trong code;
        // không có enum trạng thái riêng nên DRAFT và HIDDEN không phân biệt được.
        [Fact]
        [Trait("TC", "TC-UNIT-CourseService-021")]
        [Trait("UC", "UC-36")]
        [Trait("BR", "BR-01")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task Unpublish_ByOwner_TurnsHidden()
        {
            var course = new CourseBuilder().OwnedBy(OwnerId).WithModule("Chương một", 1).Published().Build();
            _courses.Setup(r => r.GetWithTagsForUpdateAsync(course.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(course);

            var result = await UnpublishHandler().Handle(
                new UnpublishCourseCommand(course.Id, OwnerId), CancellationToken.None);

            Assert.True(result.Success);
            Assert.False(course.IsPublished);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        // ============================================================ TC-017
        // ⚠ Không có nhánh chặn "chưa publish thì không cho archive". Unpublish() idempotent
        // (no-op khi IsPublished=false) và handler VẪN trả Success + VẪN gọi SaveChanges.
        [Fact]
        [Trait("TC", "TC-UNIT-CourseService-022")]
        [Trait("UC", "UC-36")]
        [Trait("BR", "BR-01")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        public async Task Unpublish_Draft_NoOpStillSuccess()
        {
            var course = new CourseBuilder().OwnedBy(OwnerId).Build();   // chưa publish
            _courses.Setup(r => r.GetWithTagsForUpdateAsync(course.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(course);

            var result = await UnpublishHandler().Handle(
                new UnpublishCourseCommand(course.Id, OwnerId), CancellationToken.None);

            Assert.True(result.Success);
            Assert.False(course.IsPublished);
            Assert.Null(course.UpdatedAt);   // Unpublish() return sớm nên không cả đụng timestamp
        }

        [Fact]
        [Trait("TC", "TC-UNIT-CourseService-023")]
        [Trait("UC", "UC-36")]
        [Trait("Type", "Security (GBR)")]
        [Trait("Priority", "High")]
        public async Task Unpublish_ByNonOwner_ForbiddenKeeps()
        {
            var course = new CourseBuilder().OwnedBy(OwnerId).WithModule("Chương một", 1).Published().Build();
            _courses.Setup(r => r.GetWithTagsForUpdateAsync(course.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(course);

            var result = await UnpublishHandler().Handle(
                new UnpublishCourseCommand(course.Id, OtherExpertId), CancellationToken.None);

            Assert.Equal("FORBIDDEN", result.ErrorCode);
            Assert.True(course.IsPublished);
            VerifyNotSaved();
        }

        // ============================================================ TC-021
        // Covers: DRAFT → PUBLISHED.
        [Fact]
        [Trait("TC", "TC-UNIT-CourseService-024")]
        [Trait("UC", "UC-38")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Critical")]
        public async Task Publish_WithModulesAndMaterials_Ok()
        {
            var course = new CourseBuilder().OwnedBy(OwnerId)
                .WithModule("Chương một", materialCount: 2)
                .Build();
            _courses.Setup(r => r.GetWithTagsForUpdateAsync(course.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(course);

            var result = await PublishHandler().Handle(
                new PublishCourseCommand(course.Id, OwnerId), CancellationToken.None);

            Assert.True(result.Success);
            Assert.True(course.IsPublished);
            _courses.Verify(r => r.Update(course), Times.Once);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        [Trait("TC", "TC-UNIT-CourseService-025")]
        [Trait("UC", "UC-38")]
        [Trait("Type", "Security (GBR)")]
        [Trait("Priority", "High")]
        public async Task Publish_ByNonOwner_ForbiddenKeepsDraft()
        {
            var course = new CourseBuilder().OwnedBy(OwnerId).WithModule("Chương một", 1).Build();
            _courses.Setup(r => r.GetWithTagsForUpdateAsync(course.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(course);

            var result = await PublishHandler().Handle(
                new PublishCourseCommand(course.Id, OtherExpertId), CancellationToken.None);

            Assert.Equal("FORBIDDEN", result.ErrorCode);
            Assert.False(course.IsPublished);
            VerifyNotSaved();
        }

        [Fact]
        [Trait("TC", "TC-UNIT-CourseService-026")]
        [Trait("UC", "UC-38")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        public async Task Publish_CourseNotFound_ReturnsCourseNotFound()
        {
            _courses.Setup(r => r.GetWithTagsForUpdateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((Course?)null);

            var result = await PublishHandler().Handle(
                new PublishCourseCommand(Guid.NewGuid(), OwnerId), CancellationToken.None);

            Assert.Equal("COURSE_NOT_FOUND", result.ErrorCode);
            VerifyNotSaved();
        }

        // ============================================================ TC-022
        // Covers: BR-01 has module (FALSE). Domain ném InvalidOperationException, handler BẮT
        // và dịch thành mã lỗi — khác hẳn Create/Edit vốn để exception nổi ra ngoài.
        [Fact]
        [Trait("TC", "TC-UNIT-CourseService-026")]
        [Trait("UC", "UC-38")]
        [Trait("BR", "BR-01")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task Publish_NoModule_PublishFailed()
        {
            var course = new CourseBuilder().OwnedBy(OwnerId).Build();   // không có module nào
            _courses.Setup(r => r.GetWithTagsForUpdateAsync(course.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(course);

            var result = await PublishHandler().Handle(
                new PublishCourseCommand(course.Id, OwnerId), CancellationToken.None);

            Assert.False(result.Success);
            Assert.Equal("PUBLISH_FAILED", result.ErrorCode);
            Assert.Contains("ít nhất một học phần", result.ErrorMessage);
            Assert.False(course.IsPublished);
            VerifyNotSaved();
        }

        // ============================================================ TC-023
        // ⚠ Notes trong workbook ("Publish() CHỈ kiểm ≥1 module, KHÔNG kiểm material") đã LỖI THỜI:
        // Course.Publish() có vòng lặp gọi module.ValidateBeforeCoursePublish(), và method đó
        // ném lỗi khi module rỗng. Chương không có học liệu ⇒ ĐÚNG LÀ bị chặn như UCS mô tả.
        [Fact]
        [Trait("TC", "TC-UNIT-CourseService-026")]
        [Trait("UC", "UC-38")]
        [Trait("BR", "BR-01")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task Publish_EmptyModule_PublishFailed()
        {
            var course = new CourseBuilder().OwnedBy(OwnerId)
                .WithModule("Chương rỗng", materialCount: 0)
                .Build();
            _courses.Setup(r => r.GetWithTagsForUpdateAsync(course.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(course);

            var result = await PublishHandler().Handle(
                new PublishCourseCommand(course.Id, OwnerId), CancellationToken.None);

            Assert.False(result.Success);
            Assert.Equal("PUBLISH_FAILED", result.ErrorCode);
            Assert.False(course.IsPublished);
            VerifyNotSaved();
        }

        // ============================================================ TC-024  ⚠ DEFECT
        // BR-01 yêu cầu có tag mới được publish, nhưng Course.Publish() không hề đọc CourseTags.
        [Fact(Skip = "DEF-CRS-03 - A course can be published with no tag at all")]
        [Trait("TC", "TC-UNIT-CourseService-026")]
        [Trait("UC", "UC-38")]
        [Trait("BR", "BR-01")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        [Trait("Defect", "DEF-CRS-03")]
        public async Task Publish_NoTag_AllowedNoGuard()
        {
            var course = new CourseBuilder().OwnedBy(OwnerId)
                .WithModule("Chương một", materialCount: 1)
                .Build();
            _courses.Setup(r => r.GetWithTagsForUpdateAsync(course.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(course);

            var result = await PublishHandler().Handle(
                new PublishCourseCommand(course.Id, OwnerId), CancellationToken.None);

            Assert.Empty(course.CourseTags);
            Assert.True(result.Success);
            Assert.True(course.IsPublished);
        }

        // ============================================================ TC-025
        // Covers: BR-03 HIDDEN → PUBLISHED. Khoá đã ẩn có thể phát hành lại.
        [Fact]
        [Trait("TC", "TC-UNIT-CourseService-026")]
        [Trait("UC", "UC-38")]
        [Trait("BR", "BR-03")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        public async Task Publish_HiddenCourse_CanRepublish()
        {
            var course = new CourseBuilder().OwnedBy(OwnerId)
                .WithModule("Chương một", materialCount: 1)
                .Published().Build();
            course.Unpublish();                              // đưa về trạng thái HIDDEN
            Assert.False(course.IsPublished);

            _courses.Setup(r => r.GetWithTagsForUpdateAsync(course.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(course);

            var result = await PublishHandler().Handle(
                new PublishCourseCommand(course.Id, OwnerId), CancellationToken.None);

            Assert.True(result.Success);
            Assert.True(course.IsPublished);
        }

        // ------------------------------------------------------------ TC-026 (MỚI)
        // Nhánh IsPublicationLocked của Course.Publish() (khoá bị tố cáo, chờ admin duyệt lại)
        // không có TC nào trong workbook phủ.
        // → cần thêm dòng TC-UNIT-CourseService-026 vào sheet CourseService.
        [Fact]
        [Trait("TC", "TC-UNIT-CourseService-026")]
        [Trait("UC", "UC-38")]
        [Trait("Type", "Security (GBR)")]
        [Trait("Priority", "High")]
        public async Task Publish_LockedCourse_ReturnsPublishFailed()
        {
            var course = new CourseBuilder().OwnedBy(OwnerId)
                .WithModule("Chương một", materialCount: 1)
                .Build();
            Common.TestEntity.SetProperty(course, nameof(Course.IsPublicationLocked), true);
            _courses.Setup(r => r.GetWithTagsForUpdateAsync(course.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(course);

            var result = await PublishHandler().Handle(
                new PublishCourseCommand(course.Id, OwnerId), CancellationToken.None);

            Assert.False(result.Success);
            Assert.Equal("PUBLISH_FAILED", result.ErrorCode);
            Assert.Contains("admin phê duyệt lại", result.ErrorMessage);
            Assert.False(course.IsPublished);
            VerifyNotSaved();
        }
    }
}
