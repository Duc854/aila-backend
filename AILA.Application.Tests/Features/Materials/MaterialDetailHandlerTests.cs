using AILA.Application.Common.Interfaces;
using AILA.Application.Common.Interfaces.Repositories;
using AILA.Application.Features.DocumentMaterials.Queries.GetDocumentDetail;
using AILA.Application.Features.LearningMaterials.Commands.UpdateLearningMaterial;
using AILA.Application.Features.LearningMaterials.Queries.GetLearningMaterialsByModule;
using AILA.Application.Features.Questions.Commands.ReorderQuestions;
using AILA.Application.Features.Questions.Dtos;
using AILA.Application.Features.Questions.Queries.GetQuestions;
using AILA.Application.Features.QuizMaterials.Queries.GetQuizDetail;
using AILA.Application.Features.VideoMaterials.Queries.GetVideoDetail;
using AILA.Application.Tests.Common;
using AILA.Application.Tests.Common.Builders;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using Moq;

namespace AILA.Application.Tests.Features.Materials
{
    /// <summary>
    /// Sheet: MaterialService (UC-44, UC-47, UC-49) · QuizService (UC-51, UC-56).
    /// TC-UNIT-MaterialService-021 → 029 · TC-UNIT-QuizService-039 → 047.
    ///
    /// Ba query chi tiết (Document/Video/Quiz) dùng chung một khuôn hai bước:
    ///   1. tra bảng chi tiết theo đúng loại — thấy thì kiểm quyền rồi trả DTO
    ///   2. không thấy thì tra Material chung để phân biệt "không tồn tại" với "sai loại"
    /// Vì thế nhánh INVALID_TYPE chỉ chạm được khi mock bước 1 trả null còn bước 2 trả material.
    /// </summary>
    public class MaterialDetailHandlerTests
    {
        private static readonly Guid OwnerId = Guid.NewGuid();
        private static readonly Guid OtherExpertId = Guid.NewGuid();

        private readonly Mock<IMaterialRepository> _materials = new();
        private readonly Mock<IModuleRepository> _modules = new();
        private readonly Mock<IQuestionRepository> _questions = new();
        private readonly Mock<IUnitOfWork> _uow = new();

        public MaterialDetailHandlerTests()
        {
            _uow.Setup(u => u.Materials).Returns(_materials.Object);
            _uow.Setup(u => u.Modules).Returns(_modules.Object);
            _uow.Setup(u => u.Questions).Returns(_questions.Object);
        }

        /// <summary>Module kèm navigation Course, thuộc về <paramref name="ownerId"/>.</summary>
        private static Module ModuleWithCourse(Guid ownerId)
        {
            var course = new CourseBuilder().OwnedBy(ownerId).Build();
            var module = new Module(course.Id, "Chương mở đầu", 1);
            TestEntity.SetProperty(module, nameof(Module.Course), course);
            return module;
        }

        private static Material MaterialWithParents(Guid ownerId, MaterialType type, string title = "Học liệu 1")
        {
            var module = ModuleWithCourse(ownerId);
            var material = type switch
            {
                MaterialType.Document => Material.CreateDocument(module.Id, title, 1),
                MaterialType.Video => Material.CreateVideo(module.Id, title, 1),
                MaterialType.Quiz => Material.CreateQuiz(module.Id, title, 1),
                _ => Material.CreateAiPractice(module.Id, title, 1),
            };
            TestEntity.SetProperty(material, nameof(Material.Module), module);
            return material;
        }

        // ============================================================ UC-44 Document detail

        // ------------------------------------------------------------ TC-Material-021
        [Fact]
        [Trait("TC", "TC-UNIT-MaterialService-021")]
        [Trait("UC", "UC-44")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task DocumentDetail_Owner_ReturnsContent()
        {
            var material = MaterialWithParents(OwnerId, MaterialType.Document, "Bài đọc 1");
            var document = new DocumentMaterial(material.Id, "Nội dung tài liệu.");
            TestEntity.SetProperty(document, nameof(DocumentMaterial.Material), material);

            _materials.Setup(r => r.GetDocumentDetailForExpertAsync(material.Id, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(document);

            var handler = new GetDocumentDetailQueryHandler(_uow.Object);
            var result = await handler.Handle(
                new GetDocumentDetailQuery(material.Id, OwnerId), CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal("Nội dung tài liệu.", result.Data!.Content);
        }

        // ------------------------------------------------------------ TC-Material-022
        // Covers: BR-01 — ba nhánh từ chối: sai chủ, không tồn tại, và sai loại học liệu.
        [Fact]
        [Trait("TC", "TC-UNIT-MaterialService-022")]
        [Trait("UC", "UC-44")]
        [Trait("BR", "BR-01")]
        [Trait("Type", "Security (GBR)")]
        [Trait("Priority", "Critical")]
        public async Task DocumentDetail_ForeignMissingOrWrongType_Rejected()
        {
            var handler = new GetDocumentDetailQueryHandler(_uow.Object);

            // (a) tài liệu của expert khác
            var foreign = MaterialWithParents(OtherExpertId, MaterialType.Document);
            var foreignDoc = new DocumentMaterial(foreign.Id, "Bí mật.");
            TestEntity.SetProperty(foreignDoc, nameof(DocumentMaterial.Material), foreign);
            _materials.Setup(r => r.GetDocumentDetailForExpertAsync(foreign.Id, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(foreignDoc);
            var denied = await handler.Handle(
                new GetDocumentDetailQuery(foreign.Id, OwnerId), CancellationToken.None);
            Assert.Equal("FORBIDDEN", denied.ErrorCode);
            Assert.Null(denied.Data);

            // (b) không có gì cả
            _materials.Setup(r => r.GetDocumentDetailForExpertAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync((DocumentMaterial?)null);
            _materials.Setup(r => r.GetWithModuleAndCourseAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync((Material?)null);
            var missing = await handler.Handle(
                new GetDocumentDetailQuery(Guid.NewGuid(), OwnerId), CancellationToken.None);
            Assert.Equal("MATERIAL_NOT_FOUND", missing.ErrorCode);

            // (c) học liệu có thật nhưng là Video -> INVALID_TYPE, không phải NOT_FOUND
            var video = MaterialWithParents(OwnerId, MaterialType.Video);
            _materials.Setup(r => r.GetWithModuleAndCourseAsync(video.Id, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(video);
            var wrongType = await handler.Handle(
                new GetDocumentDetailQuery(video.Id, OwnerId), CancellationToken.None);
            Assert.Equal("INVALID_TYPE", wrongType.ErrorCode);
        }

        // ============================================================ UC-47 Video detail

        // ------------------------------------------------------------ TC-Material-023
        [Fact]
        [Trait("TC", "TC-UNIT-MaterialService-023")]
        [Trait("UC", "UC-47")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task VideoDetail_Owner_ReturnsUrl()
        {
            var material = MaterialWithParents(OwnerId, MaterialType.Video, "Bài giảng 1");
            var video = new VideoMaterial(material.Id, "https://youtu.be/abc123", 600, "Tóm tắt");
            TestEntity.SetProperty(video, nameof(VideoMaterial.Material), material);

            _materials.Setup(r => r.GetVideoDetailForExpertAsync(material.Id, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(video);

            var handler = new GetVideoDetailQueryHandler(_uow.Object);
            var result = await handler.Handle(
                new GetVideoDetailQuery(material.Id, OwnerId), CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal("https://youtu.be/abc123", result.Data!.VideoUrl);
        }

        // ------------------------------------------------------------ TC-Material-024
        [Fact]
        [Trait("TC", "TC-UNIT-MaterialService-024")]
        [Trait("UC", "UC-47")]
        [Trait("BR", "BR-01")]
        [Trait("Type", "Security (GBR)")]
        [Trait("Priority", "Critical")]
        public async Task VideoDetail_ForeignMissingOrWrongType_Rejected()
        {
            var handler = new GetVideoDetailQueryHandler(_uow.Object);

            var foreign = MaterialWithParents(OtherExpertId, MaterialType.Video);
            var foreignVideo = new VideoMaterial(foreign.Id, "https://youtu.be/secret", 60);
            TestEntity.SetProperty(foreignVideo, nameof(VideoMaterial.Material), foreign);
            _materials.Setup(r => r.GetVideoDetailForExpertAsync(foreign.Id, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(foreignVideo);
            Assert.Equal("FORBIDDEN",
                (await handler.Handle(new GetVideoDetailQuery(foreign.Id, OwnerId), CancellationToken.None)).ErrorCode);

            _materials.Setup(r => r.GetVideoDetailForExpertAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync((VideoMaterial?)null);
            _materials.Setup(r => r.GetWithModuleAndCourseAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync((Material?)null);
            Assert.Equal("MATERIAL_NOT_FOUND",
                (await handler.Handle(new GetVideoDetailQuery(Guid.NewGuid(), OwnerId), CancellationToken.None)).ErrorCode);

            var document = MaterialWithParents(OwnerId, MaterialType.Document);
            _materials.Setup(r => r.GetWithModuleAndCourseAsync(document.Id, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(document);
            Assert.Equal("INVALID_TYPE",
                (await handler.Handle(new GetVideoDetailQuery(document.Id, OwnerId), CancellationToken.None)).ErrorCode);
        }

        // ============================================================ UC-51 Quiz detail

        // ------------------------------------------------------------ TC-Quiz-039
        [Fact]
        [Trait("TC", "TC-UNIT-QuizService-039")]
        [Trait("UC", "UC-51")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task QuizDetail_Owner_ReturnsSettings()
        {
            var material = MaterialWithParents(OwnerId, MaterialType.Quiz, "Bài kiểm tra");
            var quiz = new QuizMaterial(material.Id, 45, 80, true);
            TestEntity.SetProperty(quiz, nameof(QuizMaterial.Material), material);

            _materials.Setup(r => r.GetQuizDetailForExpertAsync(material.Id, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(quiz);

            var handler = new GetQuizDetailQueryHandler(_uow.Object);
            var result = await handler.Handle(
                new GetQuizDetailQuery(material.Id, OwnerId), CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal(45, result.Data!.TimeLimitMinutes);
            Assert.Equal(80, result.Data.PassingScore);
        }

        // ------------------------------------------------------------ TC-Quiz-040
        [Fact]
        [Trait("TC", "TC-UNIT-QuizService-040")]
        [Trait("UC", "UC-51")]
        [Trait("BR", "BR-01")]
        [Trait("Type", "Security (GBR)")]
        [Trait("Priority", "Critical")]
        public async Task QuizDetail_ForeignMissingOrWrongType_Rejected()
        {
            var handler = new GetQuizDetailQueryHandler(_uow.Object);

            var foreign = MaterialWithParents(OtherExpertId, MaterialType.Quiz);
            var foreignQuiz = new QuizMaterial(foreign.Id, 30, 70, true);
            TestEntity.SetProperty(foreignQuiz, nameof(QuizMaterial.Material), foreign);
            _materials.Setup(r => r.GetQuizDetailForExpertAsync(foreign.Id, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(foreignQuiz);
            Assert.Equal("FORBIDDEN",
                (await handler.Handle(new GetQuizDetailQuery(foreign.Id, OwnerId), CancellationToken.None)).ErrorCode);

            _materials.Setup(r => r.GetQuizDetailForExpertAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync((QuizMaterial?)null);
            _materials.Setup(r => r.GetWithModuleAndCourseAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync((Material?)null);
            Assert.Equal("MATERIAL_NOT_FOUND",
                (await handler.Handle(new GetQuizDetailQuery(Guid.NewGuid(), OwnerId), CancellationToken.None)).ErrorCode);

            var document = MaterialWithParents(OwnerId, MaterialType.Document);
            _materials.Setup(r => r.GetWithModuleAndCourseAsync(document.Id, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(document);
            Assert.Equal("INVALID_TYPE",
                (await handler.Handle(new GetQuizDetailQuery(document.Id, OwnerId), CancellationToken.None)).ErrorCode);
        }

        // ============================================================ UC-44 Update material

        // ------------------------------------------------------------ TC-Material-025
        [Fact]
        [Trait("TC", "TC-UNIT-MaterialService-025")]
        [Trait("UC", "UC-44")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task UpdateMaterial_Owner_TitleChanged()
        {
            var material = MaterialWithParents(OwnerId, MaterialType.Document, "Tên cũ");
            _materials.Setup(r => r.GetWithModuleAndCourseAsync(material.Id, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(material);

            var handler = new UpdateLearningMaterialCommandHandler(_uow.Object);
            var result = await handler.Handle(
                new UpdateLearningMaterialCommand(material.Id, OwnerId, "Tên mới"), CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal("Tên mới", material.Title);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        // ------------------------------------------------------------ TC-Material-026
        [Fact]
        [Trait("TC", "TC-UNIT-MaterialService-026")]
        [Trait("UC", "UC-44")]
        [Trait("BR", "BR-01")]
        [Trait("Type", "Security (GBR)")]
        [Trait("Priority", "Critical")]
        public async Task UpdateMaterial_MissingOrForeign_Rejected()
        {
            var handler = new UpdateLearningMaterialCommandHandler(_uow.Object);

            _materials.Setup(r => r.GetWithModuleAndCourseAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync((Material?)null);
            Assert.Equal("MATERIAL_NOT_FOUND",
                (await handler.Handle(new UpdateLearningMaterialCommand(Guid.NewGuid(), OwnerId, "X"),
                    CancellationToken.None)).ErrorCode);

            var foreign = MaterialWithParents(OtherExpertId, MaterialType.Document, "Của người khác");
            _materials.Setup(r => r.GetWithModuleAndCourseAsync(foreign.Id, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(foreign);
            Assert.Equal("FORBIDDEN",
                (await handler.Handle(new UpdateLearningMaterialCommand(foreign.Id, OwnerId, "Đổi tên"),
                    CancellationToken.None)).ErrorCode);

            Assert.Equal("Của người khác", foreign.Title);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        // ============================================================ UC-49 Materials by module

        // ------------------------------------------------------------ TC-Material-027
        // Covers: Main Flow — thứ tự lấy từ OrderIndex chứ không theo thứ tự chèn.
        [Fact]
        [Trait("TC", "TC-UNIT-MaterialService-027")]
        [Trait("UC", "UC-49")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task MaterialsByModule_SortedByOrderIndex()
        {
            var module = ModuleWithCourse(OwnerId);
            module.AddMaterial(Material.CreateDocument(module.Id, "Thứ ba", 3));
            module.AddMaterial(Material.CreateVideo(module.Id, "Thứ nhất", 1));
            module.AddMaterial(Material.CreateQuiz(module.Id, "Thứ hai", 2));

            _modules.Setup(r => r.GetWithCourseAsync(module.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(module);

            var handler = new GetLearningMaterialsByModuleQueryHandler(_uow.Object);
            var result = await handler.Handle(
                new GetLearningMaterialsByModuleQuery(module.Id, OwnerId), CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal(new[] { "Thứ nhất", "Thứ hai", "Thứ ba" },
                result.Data!.Select(m => m.Title).ToArray());
        }

        // ------------------------------------------------------------ TC-Material-028
        [Fact]
        [Trait("TC", "TC-UNIT-MaterialService-028")]
        [Trait("UC", "UC-49")]
        [Trait("BR", "BR-01")]
        [Trait("Type", "Security (GBR)")]
        [Trait("Priority", "Critical")]
        public async Task MaterialsByModule_MissingOrForeign_Rejected()
        {
            var handler = new GetLearningMaterialsByModuleQueryHandler(_uow.Object);

            _modules.Setup(r => r.GetWithCourseAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((Module?)null);
            Assert.Equal("MODULE_NOT_FOUND",
                (await handler.Handle(new GetLearningMaterialsByModuleQuery(Guid.NewGuid(), OwnerId),
                    CancellationToken.None)).ErrorCode);

            var foreign = ModuleWithCourse(OtherExpertId);
            _modules.Setup(r => r.GetWithCourseAsync(foreign.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(foreign);
            var denied = await handler.Handle(
                new GetLearningMaterialsByModuleQuery(foreign.Id, OwnerId), CancellationToken.None);
            Assert.Equal("FORBIDDEN", denied.ErrorCode);
            Assert.Null(denied.Data);
        }

        // ------------------------------------------------------------ TC-Material-029
        [Fact]
        [Trait("TC", "TC-UNIT-MaterialService-029")]
        [Trait("UC", "UC-49")]
        [Trait("Type", "Boundary & Negative")]
        [Trait("Priority", "Medium")]
        public async Task MaterialsByModule_EmptyModule_EmptyList()
        {
            var module = ModuleWithCourse(OwnerId);
            _modules.Setup(r => r.GetWithCourseAsync(module.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(module);

            var handler = new GetLearningMaterialsByModuleQueryHandler(_uow.Object);
            var result = await handler.Handle(
                new GetLearningMaterialsByModuleQuery(module.Id, OwnerId), CancellationToken.None);

            Assert.True(result.Success);
            Assert.Empty(result.Data!);
        }

        // ============================================================ UC-56 Questions

        // ------------------------------------------------------------ TC-Quiz-041
        [Fact]
        [Trait("TC", "TC-UNIT-QuizService-041")]
        [Trait("UC", "UC-56")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task GetQuestions_Owner_ReturnsQuestions()
        {
            var material = MaterialWithParents(OwnerId, MaterialType.Quiz);
            var quiz = new QuizMaterial(material.Id, 30, 70, true);
            TestEntity.SetProperty(quiz, nameof(QuizMaterial.Material), material);

            _materials.Setup(r => r.GetQuizDetailForExpertAsync(material.Id, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(quiz);

            // Handler KHÔNG đọc quiz.Questions — nó truy vấn lại qua Questions.GetByQuizIdAsync.
            // Bỏ mock này thì repository trả null và handler ném ArgumentNullException vì
            // không có null-guard trước .Select().
            _questions.Setup(r => r.GetByQuizIdAsync(material.Id, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(new List<Question>
                      {
                          new(material.Id, "Câu 1", QuestionType.SingleChoice, 1),
                          new(material.Id, "Câu 2", QuestionType.SingleChoice, 2),
                      });

            var handler = new GetQuestionsQueryHandler(_uow.Object);
            var result = await handler.Handle(
                new GetQuestionsQuery(material.Id, OwnerId), CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal(2, result.Data!.Count);
        }

        // ------------------------------------------------------------ TC-Quiz-042
        // Covers: BR-01 — lộ câu hỏi ra ngoài là lộ luôn đáp án của cả bài kiểm tra.
        [Fact]
        [Trait("TC", "TC-UNIT-QuizService-042")]
        [Trait("UC", "UC-56")]
        [Trait("BR", "BR-01")]
        [Trait("Type", "Security (GBR)")]
        [Trait("Priority", "Critical")]
        public async Task GetQuestions_MissingOrForeign_NoLeak()
        {
            var handler = new GetQuestionsQueryHandler(_uow.Object);

            _materials.Setup(r => r.GetQuizDetailForExpertAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync((QuizMaterial?)null);
            var missing = await handler.Handle(
                new GetQuestionsQuery(Guid.NewGuid(), OwnerId), CancellationToken.None);
            Assert.Equal("QUIZ_NOT_FOUND", missing.ErrorCode);
            Assert.Null(missing.Data);

            var foreign = MaterialWithParents(OtherExpertId, MaterialType.Quiz);
            var foreignQuiz = new QuizMaterial(foreign.Id, 30, 70, true);
            TestEntity.SetProperty(foreignQuiz, nameof(QuizMaterial.Material), foreign);
            foreignQuiz.AddQuestion(new Question(foreign.Id, "Câu bí mật", QuestionType.SingleChoice, 1));
            _materials.Setup(r => r.GetQuizDetailForExpertAsync(foreign.Id, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(foreignQuiz);

            var denied = await handler.Handle(
                new GetQuestionsQuery(foreign.Id, OwnerId), CancellationToken.None);
            Assert.Equal("FORBIDDEN", denied.ErrorCode);
            Assert.Null(denied.Data);
        }

        // ------------------------------------------------------------ TC-Quiz-043
        // Covers: Main Flow — đổi thứ tự câu hỏi. Handler chạy trong transaction và dùng
        // tempOffset 1_000_000 để né va chạm unique index (QuizMaterialId, OrderIndex)
        // khi hoán vị: nếu gán thẳng, bước trung gian sẽ có hai câu cùng OrderIndex.
        [Fact]
        [Trait("TC", "TC-UNIT-QuizService-043")]
        [Trait("UC", "UC-56")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task ReorderQuestions_AppliesNewOrderInTransaction()
        {
            var material = MaterialWithParents(OwnerId, MaterialType.Quiz);
            var quiz = new QuizMaterial(material.Id, 30, 70, true);
            TestEntity.SetProperty(quiz, nameof(QuizMaterial.Material), material);

            var q1 = new Question(material.Id, "Câu 1", QuestionType.SingleChoice, 1);
            var q2 = new Question(material.Id, "Câu 2", QuestionType.SingleChoice, 2);
            var q3 = new Question(material.Id, "Câu 3", QuestionType.SingleChoice, 3);

            _materials.Setup(r => r.GetQuizDetailForExpertAsync(material.Id, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(quiz);
            _questions.Setup(r => r.GetByQuizIdAsync(material.Id, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(new List<Question> { q1, q2, q3 });

            var handler = new ReorderQuestionsCommandHandler(_uow.Object);
            var result = await handler.Handle(
                new ReorderQuestionsCommand(material.Id, OwnerId, new List<QuestionOrderItem>
                {
                    new() { QuestionId = q3.Id, NewOrderIndex = 1 },
                    new() { QuestionId = q1.Id, NewOrderIndex = 2 },
                    new() { QuestionId = q2.Id, NewOrderIndex = 3 },
                }),
                CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal(1, q3.OrderIndex);
            Assert.Equal(2, q1.OrderIndex);
            Assert.Equal(3, q2.OrderIndex);
            _uow.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
            _uow.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
            _uow.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        // ------------------------------------------------------------ TC-Quiz-044
        [Fact]
        [Trait("TC", "TC-UNIT-QuizService-044")]
        [Trait("UC", "UC-56")]
        [Trait("BR", "BR-01")]
        [Trait("Type", "Security (GBR)")]
        [Trait("Priority", "Critical")]
        public async Task ReorderQuestions_MissingOrForeign_NoTransaction()
        {
            var handler = new ReorderQuestionsCommandHandler(_uow.Object);
            var empty = new List<QuestionOrderItem>();

            _materials.Setup(r => r.GetQuizDetailForExpertAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync((QuizMaterial?)null);
            Assert.Equal("QUIZ_NOT_FOUND",
                (await handler.Handle(new ReorderQuestionsCommand(Guid.NewGuid(), OwnerId, empty),
                    CancellationToken.None)).ErrorCode);

            var foreign = MaterialWithParents(OtherExpertId, MaterialType.Quiz);
            var foreignQuiz = new QuizMaterial(foreign.Id, 30, 70, true);
            TestEntity.SetProperty(foreignQuiz, nameof(QuizMaterial.Material), foreign);
            _materials.Setup(r => r.GetQuizDetailForExpertAsync(foreign.Id, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(foreignQuiz);
            Assert.Equal("FORBIDDEN",
                (await handler.Handle(new ReorderQuestionsCommand(foreign.Id, OwnerId, empty),
                    CancellationToken.None)).ErrorCode);

            _uow.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
