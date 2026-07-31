using AILA.Application.Common.Interfaces;
using AILA.Application.Common.Interfaces.Repositories;
using AILA.Application.Features.AIPracticeMaterials.Commands.CreateAIPracticeMaterial;
using AILA.Application.Features.AIPracticeMaterials.Commands.UpdateAIPracticeMaterial;
using AILA.Application.Features.AIPracticeMaterials.Queries.GetAIPracticeMaterialDetail;
using AILA.Application.Features.Blogs.Queries.GetTopBlogs;
using AILA.Application.Features.Courses.Queries;
using AILA.Application.Features.Courses.Queries.GetTopCourses;
using AILA.Application.Features.Questions.Commands.ImportQuestions;
using AILA.Application.Features.Questions.Dtos;
using AILA.Application.Features.Questions.Queries.GetImportTemplate;
using AILA.Application.Features.QuizMaterials.Commands.BulkCreateQuiz;
using AILA.Application.Features.QuizMaterials.Dtos.BulkCreateQuiz;
using AILA.Application.Tests.Common;
using AILA.Application.Tests.Common.Builders;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using Moq;

namespace AILA.Application.Tests.Features.Courses
{
    /// <summary>
    /// Sheet: CourseService (UC-04, UC-65) · ContentService (UC-02) ·
    /// QuizService (UC-50, UC-67, UC-68) · AIPracticeService (UC-57, UC-58).
    ///
    /// Với ba handler nặng (BulkCreateQuiz, ImportQuestions, CreateAIPracticeMaterial) phần
    /// happy path chạy trong transaction và dựng cây thực thể rất sâu, nên ở L1 chỉ phủ
    /// các nhánh gác cổng — đó cũng là nơi lỗi bảo mật nằm. Luồng ghi đầy đủ thuộc về L2.
    /// </summary>
    public class CourseQueryAndImportHandlerTests
    {
        private static readonly Guid OwnerId = Guid.NewGuid();
        private static readonly Guid OtherExpertId = Guid.NewGuid();

        private readonly Mock<ICourseRepository> _courses = new();
        private readonly Mock<IBlogPostRepository> _blogs = new();
        private readonly Mock<IMaterialRepository> _materials = new();
        private readonly Mock<IModuleRepository> _modules = new();
        private readonly Mock<IAIPracticeMaterialRepository> _aiPractice = new();
        private readonly Mock<IQuestionExcelService> _excel = new();
        private readonly Mock<IUnitOfWork> _uow = new();

        public CourseQueryAndImportHandlerTests()
        {
            _uow.Setup(u => u.Courses).Returns(_courses.Object);
            _uow.Setup(u => u.BlogPosts).Returns(_blogs.Object);
            _uow.Setup(u => u.Materials).Returns(_materials.Object);
            _uow.Setup(u => u.Modules).Returns(_modules.Object);
            _uow.Setup(u => u.AIPracticeMaterials).Returns(_aiPractice.Object);
        }

        private static Module ModuleWithCourse(Guid ownerId)
        {
            var course = new CourseBuilder().OwnedBy(ownerId).Build();
            var module = new Module(course.Id, "Chương mở đầu", 1);
            TestEntity.SetProperty(module, nameof(Module.Course), course);
            return module;
        }

        private static Material QuizMaterialWithParents(Guid ownerId, MaterialType type = MaterialType.Quiz)
        {
            var module = ModuleWithCourse(ownerId);
            var material = type == MaterialType.Quiz
                ? Material.CreateQuiz(module.Id, "Bài kiểm tra", 1)
                : Material.CreateDocument(module.Id, "Tài liệu", 1);
            TestEntity.SetProperty(material, nameof(Material.Module), module);
            return material;
        }

        private static QuizMaterial QuizWithParents(Guid ownerId)
        {
            var material = QuizMaterialWithParents(ownerId);
            var quiz = new QuizMaterial(material.Id, 30, 70, true);
            TestEntity.SetProperty(quiz, nameof(QuizMaterial.Material), material);
            return quiz;
        }

        // ============================================================ UC-04 Course detail

        // ------------------------------------------------------------ TC-CourseService-032
        // Covers: Main Flow. Handler trả DTO gồm cả cây Module → Material, dùng cho trang
        // giới thiệu khoá học mà khách chưa ghi danh vẫn xem được.
        [Fact]
        [Trait("TC", "TC-UNIT-CourseService-005")]
        [Trait("UC", "UC-04")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task CourseDetail_Existing_ReturnsStructure()
        {
            var expert = new ExpertBuilder().WithFullName("Tran Chuyen Gia").Build();
            var category = new CategoryBuilder().WithName("AI cơ bản").WithOrderIndex(1).Build();

            var course = new CourseBuilder()
                .WithName("AI nhập môn")
                .OwnedBy(expert.UserId)
                .WithCategory(category.Id)
                .Published()
                .WithModule("Chương 1", materialCount: 2)
                .Build();

            // Handler đọc thẳng course.Category.Id và course.Expert.User.FullName, không null-check.
            // Runtime thật EF nạp sẵn hai navigation này; ở L1 phải gắn tay.
            TestEntity.SetProperty(course, nameof(Course.Category), category);
            TestEntity.SetProperty(course, nameof(Course.Expert), expert);

            _courses.Setup(r => r.GetCourseDetailAsync(course.Id)).ReturnsAsync(course);

            var handler = new GetCourseDetailQueryHandler(_uow.Object);
            var result = await handler.Handle(new GetCourseDetailQuery(course.Id), CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("AI nhập môn", result!.Name);
            Assert.Single(result.Modules);
            Assert.Equal(2, result.Modules.First().Materials.Count());
        }

        // ------------------------------------------------------------ TC-CourseService-033
        // Covers: AF-01 — khoá học không tồn tại trả null (API dịch thành 404),
        // không ném ngoại lệ.
        [Fact]
        [Trait("TC", "TC-UNIT-CourseService-006")]
        [Trait("UC", "UC-04")]
        [Trait("Type", "Boundary & Negative")]
        [Trait("Priority", "High")]
        public async Task CourseDetail_Missing_Null()
        {
            _courses.Setup(r => r.GetCourseDetailAsync(It.IsAny<Guid>())).ReturnsAsync((Course?)null);

            var handler = new GetCourseDetailQueryHandler(_uow.Object);
            var result = await handler.Handle(new GetCourseDetailQuery(Guid.NewGuid()), CancellationToken.None);

            Assert.Null(result);
        }

        // ============================================================ UC-65 Expert dashboard

        // ------------------------------------------------------------ TC-CourseService-034
        // ⚠ Handler đọc thẳng c.Category.Id mà KHÔNG null-check, nên khoá học thiếu navigation
        // Category sẽ ném NullReferenceException. Mock phải gắn Category vào.
        [Fact]
        [Trait("TC", "TC-UNIT-CourseService-027")]
        [Trait("UC", "UC-65")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task ExpertCourses_ReturnsPageWithRepositoryTotal()
        {
            var category = new CategoryBuilder().WithName("AI cơ bản").WithOrderIndex(1).Build();
            var course = new CourseBuilder().OwnedBy(OwnerId).WithCategory(category.Id).Build();
            TestEntity.SetProperty(course, nameof(Course.Category), category);

            _courses.Setup(r => r.GetByExpertAsync(
                        OwnerId, null, null, 1, 10, It.IsAny<CancellationToken>()))
                    .ReturnsAsync((new List<Course> { course }, 7));

            var handler = new GetExpertCoursesQueryHandler(_uow.Object);
            var result = await handler.Handle(
                new GetExpertCoursesQuery(OwnerId, null, null, 1, 10), CancellationToken.None);

            Assert.Equal(7, result.TotalItems);
            Assert.Single(result.Items);
            Assert.Equal("AI cơ bản", result.Items.First().Category.Name);
        }

        // ------------------------------------------------------------ TC-CourseService-035
        // Covers: BR-01 — bộ lọc IsPublished (true/false/null) phải xuống repository
        // nguyên vẹn; handler không được tự lọc lại trong bộ nhớ.
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [InlineData(null)]
        [Trait("TC", "TC-UNIT-CourseService-028")]
        [Trait("UC", "UC-65")]
        [Trait("BR", "BR-01")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        public async Task ExpertCourses_ForwardsPublishedFilter(bool? isPublished)
        {
            _courses.Setup(r => r.GetByExpertAsync(
                        It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<bool?>(),
                        It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((new List<Course>(), 0));

            var handler = new GetExpertCoursesQueryHandler(_uow.Object);
            await handler.Handle(
                new GetExpertCoursesQuery(OwnerId, "ai", isPublished, 2, 20), CancellationToken.None);

            _courses.Verify(r => r.GetByExpertAsync(
                OwnerId, "ai", isPublished, 2, 20, It.IsAny<CancellationToken>()), Times.Once);
        }

        // ============================================================ UC-02 Homepage

        // ------------------------------------------------------------ TC-ContentService-023
        // ⚠ Handler gọi SearchCoursesAsync với pageIndex: 0 trong khi danh mục khoá học
        // (GetCoursesQuery) dùng phân trang 1-based. Test khoá lại tham số thực tế đang gửi
        // xuống; nếu repository là 1-based thì trang 0 có thể trả rỗng — cần kiểm tra ở L2.
        [Fact]
        [Trait("TC", "TC-UNIT-ContentService-019")]
        [Trait("UC", "UC-02")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task TopCourses_ForwardsCountAsPageSize()
        {
            var category = new CategoryBuilder().WithName("AI cơ bản").Build();
            var course = new CourseBuilder().WithName("AI nhập môn").WithCategory(category.Id).Build();
            TestEntity.SetProperty(course, nameof(Course.Category), category);

            _courses.Setup(r => r.SearchCoursesAsync(null, null, null, null, 0, 3))
                    .ReturnsAsync((new List<Course> { course }, 1));

            var handler = new GetTopCoursesQueryHandler(_uow.Object);
            var result = await handler.Handle(new GetTopCoursesQuery { Count = 3 }, CancellationToken.None);

            Assert.True(result.Success);
            Assert.Single(result.Data!);
            Assert.Equal("AI nhập môn", result.Data![0].Name);
            Assert.Equal("AI cơ bản", result.Data[0].CategoryName);
            _courses.Verify(r => r.SearchCoursesAsync(null, null, null, null, 0, 3), Times.Once);
        }

        // ------------------------------------------------------------ TC-ContentService-024
        // Khoá học chưa gán danh mục vẫn phải hiển thị được (Category?.Name là null-safe ở đây,
        // khác GetExpertCoursesQueryHandler ở TC-034).
        [Fact]
        [Trait("TC", "TC-UNIT-ContentService-020")]
        [Trait("UC", "UC-02")]
        [Trait("Type", "Boundary & Negative")]
        [Trait("Priority", "Medium")]
        public async Task TopCourses_NullCategory_StillMaps()
        {
            var course = new CourseBuilder().WithName("Chưa phân loại").Build();
            _courses.Setup(r => r.SearchCoursesAsync(
                        It.IsAny<string?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(),
                        It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>()))
                    .ReturnsAsync((new List<Course> { course }, 1));

            var handler = new GetTopCoursesQueryHandler(_uow.Object);
            var result = await handler.Handle(new GetTopCoursesQuery { Count = 5 }, CancellationToken.None);

            Assert.True(result.Success);
            Assert.Null(result.Data![0].CategoryName);
        }

        // ------------------------------------------------------------ TC-ContentService-025
        // ⚠ Bất đối xứng có chủ ý cần ghi nhận: blog dùng pageNumber = 1, còn khoá học ở
        // TC-023 dùng pageIndex = 0. Một trong hai chắc chắn sai quy ước.
        [Fact]
        [Trait("TC", "TC-UNIT-ContentService-021")]
        [Trait("UC", "UC-02")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task TopBlogs_ForwardsCountAsPageSize()
        {
            var blog = new BlogPost("Artificial intelligence for everyone", "ai-for-all", "Nội dung.");
            blog.Publish();

            _blogs.Setup(r => r.GetPagedBlogsAsync(null, 1, 4, It.IsAny<CancellationToken>()))
                  .ReturnsAsync((new List<BlogPost> { blog }, 1));

            var handler = new GetTopBlogsQueryHandler(_uow.Object);
            var result = await handler.Handle(new GetTopBlogsQuery { Count = 4 }, CancellationToken.None);

            Assert.True(result.Success);
            Assert.Single(result.Data!);
            Assert.Equal("ai-for-all", result.Data![0].Slug);
            Assert.NotNull(result.Data[0].PublishedAt);
            _blogs.Verify(r => r.GetPagedBlogsAsync(null, 1, 4, It.IsAny<CancellationToken>()), Times.Once);
        }

        // ============================================================ UC-50 BulkCreateQuiz

        // ------------------------------------------------------------ TC-QuizService-045
        // Covers: BR-01 — ba cổng gác chạy TRƯỚC khi mở transaction. Nếu một trong ba lọt,
        // expert khác có thể ghi đè toàn bộ ngân hàng câu hỏi của người ta.
        [Fact]
        [Trait("TC", "TC-UNIT-QuizService-028")]
        [Trait("UC", "UC-50")]
        [Trait("BR", "BR-01")]
        [Trait("Type", "Security (GBR)")]
        [Trait("Priority", "Critical")]
        public async Task BulkCreateQuiz_MissingForeignOrWrongType_NoTransaction()
        {
            var handler = new BulkCreateQuizCommandHandler(_uow.Object);
            var questions = new List<BulkQuestionDto>();

            _materials.Setup(r => r.GetWithModuleAndCourseAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync((Material?)null);
            Assert.Equal("MATERIAL_NOT_FOUND",
                (await handler.Handle(new BulkCreateQuizCommand(Guid.NewGuid(), OwnerId, 30, 70, true, questions),
                    CancellationToken.None)).ErrorCode);

            var foreign = QuizMaterialWithParents(OtherExpertId);
            _materials.Setup(r => r.GetWithModuleAndCourseAsync(foreign.Id, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(foreign);
            Assert.Equal("FORBIDDEN",
                (await handler.Handle(new BulkCreateQuizCommand(foreign.Id, OwnerId, 30, 70, true, questions),
                    CancellationToken.None)).ErrorCode);

            var document = QuizMaterialWithParents(OwnerId, MaterialType.Document);
            _materials.Setup(r => r.GetWithModuleAndCourseAsync(document.Id, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(document);
            Assert.Equal("INVALID_TYPE",
                (await handler.Handle(new BulkCreateQuizCommand(document.Id, OwnerId, 30, 70, true, questions),
                    CancellationToken.None)).ErrorCode);

            _uow.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        // ============================================================ UC-67 Import template

        // ------------------------------------------------------------ TC-QuizService-046
        [Fact]
        [Trait("TC", "TC-UNIT-QuizService-031")]
        [Trait("UC", "UC-67")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        public async Task ImportTemplate_Owner_ReturnsFile()
        {
            var quiz = QuizWithParents(OwnerId);
            _materials.Setup(r => r.GetQuizDetailForExpertAsync(quiz.MaterialId, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(quiz);
            _excel.Setup(s => s.GenerateImportTemplate()).Returns(new byte[] { 1, 2, 3 });

            var handler = new GetImportTemplateQueryHandler(_uow.Object, _excel.Object);
            var result = await handler.Handle(
                new GetImportTemplateQuery(quiz.MaterialId, OwnerId), CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal(3, result.FileContent!.Length);
        }

        // ------------------------------------------------------------ TC-QuizService-047
        [Fact]
        [Trait("TC", "TC-UNIT-QuizService-032")]
        [Trait("UC", "UC-67")]
        [Trait("BR", "BR-01")]
        [Trait("Type", "Security (GBR)")]
        [Trait("Priority", "Medium")]
        public async Task ImportTemplate_MissingOrForeign_NoFileGenerated()
        {
            var handler = new GetImportTemplateQueryHandler(_uow.Object, _excel.Object);

            _materials.Setup(r => r.GetQuizDetailForExpertAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync((QuizMaterial?)null);
            Assert.Equal("QUIZ_NOT_FOUND",
                (await handler.Handle(new GetImportTemplateQuery(Guid.NewGuid(), OwnerId),
                    CancellationToken.None)).ErrorCode);

            var foreign = QuizWithParents(OtherExpertId);
            _materials.Setup(r => r.GetQuizDetailForExpertAsync(foreign.MaterialId, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(foreign);
            Assert.Equal("FORBIDDEN",
                (await handler.Handle(new GetImportTemplateQuery(foreign.MaterialId, OwnerId),
                    CancellationToken.None)).ErrorCode);

            _excel.Verify(s => s.GenerateImportTemplate(), Times.Never);
        }

        // ============================================================ UC-68/69 Import questions

        // ------------------------------------------------------------ TC-QuizService-048
        // Covers: AF-01 — file hỏng thì handler nuốt mọi Exception từ trình đọc Excel và
        // trả INVALID_FILE, không để lộ stack trace của thư viện ra API.
        [Fact]
        [Trait("TC", "TC-UNIT-QuizService-033")]
        [Trait("UC", "UC-68")]
        [Trait("Type", "Boundary & Negative")]
        [Trait("Priority", "High")]
        public async Task ImportQuestions_UnreadableFile_InvalidFile()
        {
            var quiz = QuizWithParents(OwnerId);
            _materials.Setup(r => r.GetQuizDetailForExpertAsync(quiz.MaterialId, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(quiz);
            _excel.Setup(s => s.ParseImportFile(It.IsAny<Stream>()))
                  .Throws(new InvalidDataException("không phải xlsx"));

            var handler = new ImportQuestionsCommandHandler(_uow.Object, _excel.Object);
            var result = await handler.Handle(
                new ImportQuestionsCommand(quiz.MaterialId, OwnerId, Stream.Null, DryRun: false),
                CancellationToken.None);

            Assert.False(result.Success);
            Assert.Equal("INVALID_FILE", result.ErrorCode);
        }

        // ------------------------------------------------------------ TC-QuizService-049
        // Covers: BR-01 — mọi dòng đều hỏng thì KHÔNG import gì cả.
        [Fact]
        [Trait("TC", "TC-UNIT-QuizService-034")]
        [Trait("UC", "UC-68")]
        [Trait("BR", "BR-01")]
        [Trait("Type", "Boundary & Negative")]
        [Trait("Priority", "High")]
        public async Task ImportQuestions_AllRowsInvalid_NoValidRows()
        {
            var quiz = QuizWithParents(OwnerId);
            _materials.Setup(r => r.GetQuizDetailForExpertAsync(quiz.MaterialId, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(quiz);
            _excel.Setup(s => s.ParseImportFile(It.IsAny<Stream>()))
                  .Returns(new List<QuestionImportRowDto>
                  {
                      new() { RowNumber = 2, IsValid = false, Errors = { "Thiếu đáp án đúng" } },
                      new() { RowNumber = 3, IsValid = false, Errors = { "Nội dung rỗng" } },
                  });

            var handler = new ImportQuestionsCommandHandler(_uow.Object, _excel.Object);
            var result = await handler.Handle(
                new ImportQuestionsCommand(quiz.MaterialId, OwnerId, Stream.Null, DryRun: false),
                CancellationToken.None);

            Assert.False(result.Success);
            Assert.Equal("NO_VALID_ROWS", result.ErrorCode);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        // ------------------------------------------------------------ TC-QuizService-050
        // Covers: UC-69 Review Imported Questions — chế độ DryRun chỉ ĐỌC và trả về kết quả
        // soát lỗi từng dòng để expert xem trước, tuyệt đối không ghi gì vào database.
        [Fact]
        [Trait("TC", "TC-UNIT-QuizService-035")]
        [Trait("UC", "UC-69")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task ImportQuestions_DryRun_ReportsPerRowWithoutWriting()
        {
            var quiz = QuizWithParents(OwnerId);
            _materials.Setup(r => r.GetQuizDetailForExpertAsync(quiz.MaterialId, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(quiz);
            _excel.Setup(s => s.ParseImportFile(It.IsAny<Stream>()))
                  .Returns(new List<QuestionImportRowDto>
                  {
                      new() { RowNumber = 2, IsValid = true,  Content = "Câu hợp lệ" },
                      new() { RowNumber = 3, IsValid = false, Errors = { "Thiếu đáp án đúng" } },
                      new() { RowNumber = 4, IsValid = true,  Content = "Câu hợp lệ 2" },
                  });

            var handler = new ImportQuestionsCommandHandler(_uow.Object, _excel.Object);
            var result = await handler.Handle(
                new ImportQuestionsCommand(quiz.MaterialId, OwnerId, Stream.Null, DryRun: true),
                CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal(3, result.Data!.TotalRows);
            Assert.Equal(2, result.Data.ValidRows);
            Assert.Equal(1, result.Data.InvalidRows);
            Assert.Contains(result.Data.Rows, r => r.RowNumber == 3 && !r.IsValid);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        // ------------------------------------------------------------ TC-QuizService-051
        [Fact]
        [Trait("TC", "TC-UNIT-QuizService-036")]
        [Trait("UC", "UC-68")]
        [Trait("BR", "BR-02")]
        [Trait("Type", "Security (GBR)")]
        [Trait("Priority", "Critical")]
        public async Task ImportQuestions_MissingOrForeign_FileNeverParsed()
        {
            var handler = new ImportQuestionsCommandHandler(_uow.Object, _excel.Object);

            _materials.Setup(r => r.GetQuizDetailForExpertAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync((QuizMaterial?)null);
            Assert.Equal("QUIZ_NOT_FOUND",
                (await handler.Handle(new ImportQuestionsCommand(Guid.NewGuid(), OwnerId, Stream.Null, false),
                    CancellationToken.None)).ErrorCode);

            var foreign = QuizWithParents(OtherExpertId);
            _materials.Setup(r => r.GetQuizDetailForExpertAsync(foreign.MaterialId, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(foreign);
            Assert.Equal("FORBIDDEN",
                (await handler.Handle(new ImportQuestionsCommand(foreign.MaterialId, OwnerId, Stream.Null, false),
                    CancellationToken.None)).ErrorCode);

            _excel.Verify(s => s.ParseImportFile(It.IsAny<Stream>()), Times.Never);
        }

        // ============================================================ UC-57/58 AI Practice

        // ------------------------------------------------------------ TC-AIPractice-018
        [Fact]
        [Trait("TC", "TC-UNIT-AIPracticeService-001")]
        [Trait("UC", "UC-57")]
        [Trait("BR", "BR-01")]
        [Trait("Type", "Security (GBR)")]
        [Trait("Priority", "Critical")]
        public async Task CreateAiPractice_MissingOrForeignModule_NoTransaction()
        {
            var handler = new CreateAIPracticeMaterialCommandHandler(_uow.Object);
            var request = new CreateAIPracticeMaterialRequestDto
            {
                ModuleId = Guid.NewGuid(),
                Title = "Luyện viết prompt",
                Scenario = "Bối cảnh",
                AiTask = "Nhiệm vụ AI",
                LearnerTask = "Nhiệm vụ học viên",
                Difficulty = PracticeDifficulty.Easy,
                MaxPromptAttempts = 3,
            };

            _modules.Setup(r => r.GetWithCourseAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((Module?)null);
            Assert.Equal("MODULE_NOT_FOUND",
                (await handler.Handle(new CreateAIPracticeMaterialCommand(OwnerId, request),
                    CancellationToken.None)).ErrorCode);

            var foreign = ModuleWithCourse(OtherExpertId);
            _modules.Setup(r => r.GetWithCourseAsync(foreign.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(foreign);
            request.ModuleId = foreign.Id;
            Assert.Equal("FORBIDDEN",
                (await handler.Handle(new CreateAIPracticeMaterialCommand(OwnerId, request),
                    CancellationToken.None)).ErrorCode);

            _uow.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        // ------------------------------------------------------------ TC-AIPractice-019
        [Fact]
        [Trait("TC", "TC-UNIT-AIPracticeService-004")]
        [Trait("UC", "UC-58")]
        [Trait("BR", "BR-01")]
        [Trait("Type", "Security (GBR)")]
        [Trait("Priority", "Critical")]
        public async Task UpdateAiPractice_MissingOrForeign_NoTransaction()
        {
            var handler = new UpdateAIPracticeMaterialCommandHandler(_uow.Object);
            var dto = new UpdateAIPracticeMaterialDto
            {
                Title = "Tên mới",
                Scenario = "Bối cảnh mới",
                AiTask = "Nhiệm vụ AI",
                LearnerTask = "Nhiệm vụ học viên",
                MaxPromptAttempts = 5,
            };

            _aiPractice.Setup(r => r.GetForUpdateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync((AIPracticeMaterial?)null);
            Assert.Equal("AI_PRACTICE_NOT_FOUND",
                (await handler.Handle(new UpdateAIPracticeMaterialCommand(OwnerId, Guid.NewGuid(), dto),
                    CancellationToken.None)).ErrorCode);

            var foreign = ForeignAiPractice();
            _aiPractice.Setup(r => r.GetForUpdateAsync(foreign.MaterialId, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(foreign);
            Assert.Equal("FORBIDDEN",
                (await handler.Handle(new UpdateAIPracticeMaterialCommand(OwnerId, foreign.MaterialId, dto),
                    CancellationToken.None)).ErrorCode);

            _uow.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        // ------------------------------------------------------------ TC-AIPractice-020
        [Fact]
        [Trait("TC", "TC-UNIT-AIPracticeService-006")]
        [Trait("UC", "UC-58")]
        [Trait("BR", "BR-01")]
        [Trait("Type", "Security (GBR)")]
        [Trait("Priority", "Critical")]
        public async Task AiPracticeDetail_MissingOrForeign_Rejected()
        {
            var handler = new GetAIPracticeMaterialDetailQueryHandler(_uow.Object);

            _aiPractice.Setup(r => r.GetDetailForExpertAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync((AIPracticeMaterial?)null);
            var missing = await handler.Handle(
                new GetAIPracticeMaterialDetailQuery(OwnerId, Guid.NewGuid()), CancellationToken.None);
            Assert.Equal("SCENARIO_NOT_FOUND", missing.ErrorCode);
            Assert.Null(missing.Data);

            var foreign = ForeignAiPractice();
            _aiPractice.Setup(r => r.GetDetailForExpertAsync(foreign.MaterialId, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(foreign);
            var denied = await handler.Handle(
                new GetAIPracticeMaterialDetailQuery(OwnerId, foreign.MaterialId), CancellationToken.None);
            Assert.Equal("FORBIDDEN", denied.ErrorCode);
            Assert.Null(denied.Data);
        }

        /// <summary>AIPracticeMaterial thuộc về expert KHÁC, kèm navigation lên Course.</summary>
        private static AIPracticeMaterial ForeignAiPractice()
        {
            var module = ModuleWithCourse(OtherExpertId);
            var material = Material.CreateAiPractice(module.Id, "Kịch bản của người khác", 1);
            TestEntity.SetProperty(material, nameof(Material.Module), module);

            var aiPractice = new AIPracticeMaterial(
                material.Id, "Bối cảnh", "Nhiệm vụ AI", "Nhiệm vụ học viên",
                PracticeDifficulty.Easy, 3);
            TestEntity.SetProperty(aiPractice, nameof(AIPracticeMaterial.Material), material);

            return aiPractice;
        }
    }
}
