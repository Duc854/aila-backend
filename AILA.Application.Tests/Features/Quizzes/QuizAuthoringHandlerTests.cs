using AILA.Application.Common.Interfaces;
using AILA.Application.Common.Interfaces.Repositories;
using AILA.Application.Features.Questions.Commands.CreateQuestion;
using AILA.Application.Features.Questions.Commands.DeleteQuestion;
using AILA.Application.Features.Questions.Commands.UpdateQuestion;
using AILA.Application.Features.QuizMaterials.Commands.UpdateQuizDetail;
using AILA.Application.Tests.Common;
using AILA.Application.Tests.Common.Builders;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using Moq;

namespace AILA.Application.Tests.Features.Quizzes
{
    /// <summary>
    /// Sheet: QuizService · UC-50 → UC-56 · TC-UNIT-QuizService-001 → 023.
    ///
    /// Cấu trúc thật của tính năng (workbook ghi đúng):
    ///   Quiz  = Material(type=Quiz) + QuizMaterial (cài đặt TimeLimit/PassingScore)  → 2 bước
    ///   Câu hỏi thêm riêng qua CreateQuestion; đáp án thêm riêng qua feature AnswerOptions.
    /// Nên "createQuiz" và "updateQuiz" của workbook cùng ánh xạ về UpdateQuizDetail (upsert),
    /// còn "removeQuiz" dùng chung DeleteLearningMaterial với MaterialService.
    /// </summary>
    public class QuizAuthoringHandlerTests
    {
        private static readonly Guid OwnerId = Guid.NewGuid();
        private static readonly Guid OtherExpertId = Guid.NewGuid();

        private readonly Mock<IMaterialRepository> _materials = new();
        private readonly Mock<IQuestionRepository> _questions = new();
        private readonly Mock<IGenericRepository<QuizMaterial>> _quizRepo = new();
        private readonly Mock<IUnitOfWork> _uow = new();

        public QuizAuthoringHandlerTests()
        {
            _uow.Setup(u => u.Materials).Returns(_materials.Object);
            _uow.Setup(u => u.Questions).Returns(_questions.Object);
            _uow.Setup(u => u.Repository<QuizMaterial>()).Returns(_quizRepo.Object);
        }

        private UpdateQuizDetailCommandHandler QuizHandler() => new(_uow.Object);
        private CreateQuestionCommandHandler CreateQuestionHandler() => new(_uow.Object);
        private UpdateQuestionCommandHandler UpdateQuestionHandler() => new(_uow.Object);
        private DeleteQuestionCommandHandler DeleteQuestionHandler() => new(_uow.Object);

        private void VerifyNotSaved()
            => _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

        /// <summary>Material(type=Quiz) + navigation Module → Course.</summary>
        private static Material QuizMaterialShell(Guid ownerId, bool coursePublished = false)
        {
            var builder = new CourseBuilder().OwnedBy(ownerId);
            var course = coursePublished ? builder.Published().Build() : builder.Build();

            var module = new Module(course.Id, "Chương mở đầu", 1);
            TestEntity.SetProperty(module, nameof(Module.Course), course);

            var material = Material.CreateQuiz(module.Id, "Bài kiểm tra số 1", 1);
            TestEntity.SetProperty(material, nameof(Material.Module), module);

            return material;
        }

        /// <summary>QuizMaterial đã tồn tại, kèm navigation Material → Module → Course.</summary>
        private static QuizMaterial ExistingQuiz(
            Guid ownerId, bool coursePublished = false, int timeLimit = 30, decimal passingScore = 70)
        {
            var material = QuizMaterialShell(ownerId, coursePublished);
            var quiz = new QuizMaterial(material.Id, timeLimit, passingScore, true);
            TestEntity.SetProperty(quiz, nameof(QuizMaterial.Material), material);
            return quiz;
        }

        /// <summary>Question kèm navigation QuizMaterial → Material → Module → Course.</summary>
        private static Question QuestionWithParents(
            Guid ownerId, bool coursePublished = false, int orderIndex = 1)
        {
            var quiz = ExistingQuiz(ownerId, coursePublished);
            var question = new Question(quiz.MaterialId, "1 + 1 = ?", QuestionType.SingleChoice, orderIndex);
            TestEntity.SetProperty(question, nameof(Question.QuizMaterial), quiz);
            return question;
        }

        // ============================================================ TC-001
        // Covers: BR-03 — quiz tạo được mà KHÔNG cần câu hỏi nào. Nhánh upsert "chưa có
        // QuizMaterial" → tạo mới từ Material vỏ đã có sẵn.
        [Fact]
        [Trait("TC", "TC-UNIT-QuizService-001")]
        [Trait("UC", "UC-50")]
        [Trait("BR", "BR-03")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task UpsertQuiz_NoDetailYet_NoQuestionNeeded()
        {
            var material = QuizMaterialShell(OwnerId);
            _materials.Setup(r => r.GetQuizDetailForExpertAsync(material.Id, It.IsAny<CancellationToken>()))
                      .ReturnsAsync((QuizMaterial?)null);
            _materials.Setup(r => r.GetWithModuleAndCourseAsync(material.Id, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(material);

            QuizMaterial? created = null;
            _quizRepo.Setup(r => r.AddAsync(It.IsAny<QuizMaterial>()))
                     .Callback<QuizMaterial>(q =>
                     {
                         created = q;
                         // Mô phỏng relationship fixup của EF Core: khi cả Material lẫn
                         // QuizMaterial mới cùng được change tracker theo dõi, EF tự gán
                         // navigation Material. Handler PHỤ THUỘC vào hành vi ngầm này —
                         // ngay sau khi lưu nó gọi QuizMaterialMapper.MapToDto(newQuiz),
                         // mà mapper đọc entity.Material.Title. Không có fixup thì NRE.
                         // Xem DEF-QZ-04.
                         TestEntity.SetProperty(q, nameof(QuizMaterial.Material), material);
                     })
                     .Returns(Task.CompletedTask);

            var result = await QuizHandler().Handle(
                new UpdateQuizDetailCommand(material.Id, OwnerId, 30, 70, true), CancellationToken.None);

            Assert.True(result.Success);
            Assert.NotNull(created);
            Assert.Equal(30, created!.TimeLimitMinutes);
            Assert.Equal(70, created.PassingScore);
            Assert.Empty(created.Questions);           // quiz hợp lệ dù chưa có câu hỏi nào
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        // Học liệu không phải loại Quiz thì không được gắn cài đặt quiz.
        [Fact]
        [Trait("TC", "TC-UNIT-QuizService-002")]
        [Trait("UC", "UC-50")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        public async Task UpsertQuiz_NotQuizType_InvalidType()
        {
            var course = new CourseBuilder().OwnedBy(OwnerId).Build();
            var module = new Module(course.Id, "Chương mở đầu", 1);
            TestEntity.SetProperty(module, nameof(Module.Course), course);
            var document = Material.CreateDocument(module.Id, "Tài liệu mở đầu", 1);
            TestEntity.SetProperty(document, nameof(Material.Module), module);

            _materials.Setup(r => r.GetQuizDetailForExpertAsync(document.Id, It.IsAny<CancellationToken>()))
                      .ReturnsAsync((QuizMaterial?)null);
            _materials.Setup(r => r.GetWithModuleAndCourseAsync(document.Id, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(document);

            var result = await QuizHandler().Handle(
                new UpdateQuizDetailCommand(document.Id, OwnerId, 30, 70, true), CancellationToken.None);

            Assert.Equal("INVALID_TYPE", result.ErrorCode);
            VerifyNotSaved();
        }

        // ============================================================ TC-002 / TC-004
        // Covers: BR-01 — TimeLimit phải > 0. Cả nhánh tạo mới lẫn nhánh cập nhật đều
        // dùng chung ràng buộc của QuizMaterial.
        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        [Trait("TC", "TC-UNIT-QuizService-003")]
        [Trait("UC", "UC-50")]
        [Trait("BR", "BR-01")]
        [Trait("Type", "Boundary & Negative")]
        [Trait("Priority", "Medium")]
        public async Task UpsertQuiz_NonPositiveTime_Throws(int timeLimit)
        {
            var quiz = ExistingQuiz(OwnerId);
            _materials.Setup(r => r.GetQuizDetailForExpertAsync(quiz.MaterialId, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(quiz);

            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => QuizHandler().Handle(
                    new UpdateQuizDetailCommand(quiz.MaterialId, OwnerId, timeLimit, 70, true),
                    CancellationToken.None));

            Assert.Contains("lớn hơn 0 phút", ex.Message);
            Assert.Equal(30, quiz.TimeLimitMinutes);   // không bị đổi một phần
            VerifyNotSaved();
        }

        // ============================================================ TC-003
        // Covers: BVA PassingScore.
        // ⚠ UCS ghi min = 1, code cho phép 0. Test bám CODE (0 hợp lệ) — chênh lệch này đã
        // được ghi nhận trong Notes của workbook.
        [Theory]
        [InlineData(0)]      // biên dưới theo code
        [InlineData(1)]
        [InlineData(100)]    // biên trên
        [Trait("TC", "TC-UNIT-QuizService-004")]
        [Trait("UC", "UC-50")]
        [Trait("BR", "BR-01")]
        [Trait("Type", "Boundary & Negative")]
        [Trait("Priority", "Medium")]
        public async Task UpsertQuiz_PassingScoreWithinRange_IsAccepted(int passingScore)
        {
            var quiz = ExistingQuiz(OwnerId);
            _materials.Setup(r => r.GetQuizDetailForExpertAsync(quiz.MaterialId, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(quiz);

            var result = await QuizHandler().Handle(
                new UpdateQuizDetailCommand(quiz.MaterialId, OwnerId, 30, passingScore, true),
                CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal(passingScore, quiz.PassingScore);
        }

        [Theory]
        [InlineData(-1)]     // biên dưới - 1
        [InlineData(101)]    // biên trên + 1
        [InlineData(150)]
        [Trait("TC", "TC-UNIT-QuizService-005")]
        [Trait("UC", "UC-50")]
        [Trait("Type", "Boundary & Negative")]
        [Trait("Priority", "Medium")]
        public async Task UpsertQuiz_ScoreOutOfRange_Throws(int passingScore)
        {
            var quiz = ExistingQuiz(OwnerId, passingScore: 70);
            _materials.Setup(r => r.GetQuizDetailForExpertAsync(quiz.MaterialId, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(quiz);

            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => QuizHandler().Handle(
                    new UpdateQuizDetailCommand(quiz.MaterialId, OwnerId, 30, passingScore, true),
                    CancellationToken.None));

            Assert.Contains("từ 0 đến 100", ex.Message);
            Assert.Equal(70, quiz.PassingScore);
            VerifyNotSaved();
        }

        // ============================================================ TC-005
        [Fact]
        [Trait("TC", "TC-UNIT-QuizService-006")]
        [Trait("UC", "UC-51")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task UpsertQuiz_Existing_UpdatesSettings()
        {
            var quiz = ExistingQuiz(OwnerId, timeLimit: 30, passingScore: 70);
            _materials.Setup(r => r.GetQuizDetailForExpertAsync(quiz.MaterialId, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(quiz);

            var result = await QuizHandler().Handle(
                new UpdateQuizDetailCommand(quiz.MaterialId, OwnerId, 45, 80, false), CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal(45, quiz.TimeLimitMinutes);
            Assert.Equal(80, quiz.PassingScore);
            Assert.False(quiz.ShowCorrectAnswersAfterSubmission);
            _quizRepo.Verify(r => r.AddAsync(It.IsAny<QuizMaterial>()), Times.Never);   // update, không tạo mới
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        [Trait("TC", "TC-UNIT-QuizService-007")]
        [Trait("UC", "UC-51")]
        [Trait("Type", "Security (GBR)")]
        [Trait("Priority", "High")]
        public async Task UpsertQuiz_ByNonOwner_ForbiddenKeeps()
        {
            var quiz = ExistingQuiz(OwnerId, timeLimit: 30);
            _materials.Setup(r => r.GetQuizDetailForExpertAsync(quiz.MaterialId, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(quiz);

            var result = await QuizHandler().Handle(
                new UpdateQuizDetailCommand(quiz.MaterialId, OtherExpertId, 45, 80, false),
                CancellationToken.None);

            Assert.Equal("FORBIDDEN", result.ErrorCode);
            Assert.Equal(30, quiz.TimeLimitMinutes);
            VerifyNotSaved();
        }

        // ============================================================ TC-007  ⚠ DEFECT
        // Cùng lỗ hổng với DEF-MAT-01: không kiểm Course.IsPublished khi sửa cài đặt quiz.
        // Nặng hơn ở đây — đổi PassingScore của bài kiểm tra mà học viên đang làm dở.
        [Fact(Skip = "DEF-MAT-01 - Content can still be edited while the course is published")]
        [Trait("TC", "TC-UNIT-QuizService-007")]
        [Trait("UC", "UC-51")]
        [Trait("BR", "BR-02")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        [Trait("Defect", "DEF-MAT-01")]
        public async Task UpsertQuiz_PublishedCourse_NoGuard()
        {
            var quiz = ExistingQuiz(OwnerId, coursePublished: true, passingScore: 70);
            _materials.Setup(r => r.GetQuizDetailForExpertAsync(quiz.MaterialId, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(quiz);

            var result = await QuizHandler().Handle(
                new UpdateQuizDetailCommand(quiz.MaterialId, OwnerId, 30, 95, true), CancellationToken.None);

            Assert.True(quiz.Material.Module.Course.IsPublished);
            Assert.True(result.Success);
            Assert.Equal(95, quiz.PassingScore);   // nâng điểm đạt của quiz đang phát hành
        }

        // ============================================================ TC-011
        // Covers: BR-04 "thêm vào cuối" — OrderIndex tự tính = max + 1, giống
        // CreateLearningMaterial và khác CreateModule.
        [Fact]
        [Trait("TC", "TC-UNIT-QuizService-011")]
        [Trait("UC", "UC-53")]
        [Trait("BR", "BR-04")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task CreateQuestion_AppendsToEnd_NextOrder()
        {
            var quiz = ExistingQuiz(OwnerId);
            var q1 = new Question(quiz.MaterialId, "Câu 1", QuestionType.SingleChoice, 1);
            var q2 = new Question(quiz.MaterialId, "Câu 2", QuestionType.SingleChoice, 2);
            _materials.Setup(r => r.GetQuizDetailForExpertAsync(quiz.MaterialId, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(quiz);
            _questions.Setup(r => r.GetByQuizIdAsync(quiz.MaterialId, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(new List<Question> { q1, q2 });

            Question? added = null;
            _questions.Setup(r => r.AddAsync(It.IsAny<Question>()))
                      .Callback<Question>(q => added = q)
                      .Returns(Task.CompletedTask);

            var result = await CreateQuestionHandler().Handle(
                new CreateQuestionCommand(quiz.MaterialId, OwnerId, "1 + 1 = ?", QuestionType.SingleChoice),
                CancellationToken.None);

            Assert.True(result.Success);
            Assert.NotNull(added);
            Assert.Equal(3, added!.OrderIndex);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        [Trait("TC", "TC-UNIT-QuizService-012")]
        [Trait("UC", "UC-53")]
        [Trait("Type", "Boundary & Negative")]
        [Trait("Priority", "Medium")]
        public async Task CreateQuestion_EmptyQuiz_OrderAtOne()
        {
            var quiz = ExistingQuiz(OwnerId);
            _materials.Setup(r => r.GetQuizDetailForExpertAsync(quiz.MaterialId, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(quiz);
            _questions.Setup(r => r.GetByQuizIdAsync(quiz.MaterialId, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(new List<Question>());

            Question? added = null;
            _questions.Setup(r => r.AddAsync(It.IsAny<Question>()))
                      .Callback<Question>(q => added = q)
                      .Returns(Task.CompletedTask);

            await CreateQuestionHandler().Handle(
                new CreateQuestionCommand(quiz.MaterialId, OwnerId, "1 + 1 = ?", QuestionType.SingleChoice),
                CancellationToken.None);

            Assert.Equal(1, added!.OrderIndex);
        }

        [Fact]
        [Trait("TC", "TC-UNIT-QuizService-012")]
        [Trait("UC", "UC-53")]
        [Trait("Type", "Security (GBR)")]
        [Trait("Priority", "High")]
        public async Task CreateQuestion_ByNonOwner_ReturnsForbidden()
        {
            var quiz = ExistingQuiz(OwnerId);
            _materials.Setup(r => r.GetQuizDetailForExpertAsync(quiz.MaterialId, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(quiz);

            var result = await CreateQuestionHandler().Handle(
                new CreateQuestionCommand(quiz.MaterialId, OtherExpertId, "1 + 1 = ?", QuestionType.SingleChoice),
                CancellationToken.None);

            Assert.Equal("FORBIDDEN", result.ErrorCode);
            _questions.Verify(r => r.AddAsync(It.IsAny<Question>()), Times.Never);
            VerifyNotSaved();
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [Trait("TC", "TC-UNIT-QuizService-012")]
        [Trait("UC", "UC-53")]
        [Trait("Type", "Input Validation")]
        [Trait("Priority", "High")]
        public async Task CreateQuestion_BlankContent_ThrowsFromDomain(string content)
        {
            var quiz = ExistingQuiz(OwnerId);
            _materials.Setup(r => r.GetQuizDetailForExpertAsync(quiz.MaterialId, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(quiz);
            _questions.Setup(r => r.GetByQuizIdAsync(quiz.MaterialId, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(new List<Question>());

            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => CreateQuestionHandler().Handle(
                    new CreateQuestionCommand(quiz.MaterialId, OwnerId, content, QuestionType.SingleChoice),
                    CancellationToken.None));

            Assert.Equal("content", ex.ParamName);
            VerifyNotSaved();
        }

        // ============================================================ TC-012  ⚠ DEFECT
        // BR-01 "tối thiểu 2 đáp án" KHÔNG được enforce: CreateQuestion không hề đụng tới
        // đáp án — chúng thuộc feature AnswerOptions riêng. Hệ quả: quiz có thể chứa câu hỏi
        // 0 đáp án và vẫn được publish (Course.Publish chỉ kiểm module có material).
        [Fact(Skip = "DEF-QZ-01 - A question can be created with no answer option")]
        [Trait("TC", "TC-UNIT-QuizService-012")]
        [Trait("UC", "UC-53")]
        [Trait("BR", "BR-01")]
        [Trait("Type", "Boundary & Negative")]
        [Trait("Priority", "High")]
        [Trait("Defect", "DEF-QZ-01")]
        public async Task CreateQuestion_NoAnswerOption_NoGuard()
        {
            var quiz = ExistingQuiz(OwnerId);
            _materials.Setup(r => r.GetQuizDetailForExpertAsync(quiz.MaterialId, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(quiz);
            _questions.Setup(r => r.GetByQuizIdAsync(quiz.MaterialId, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(new List<Question>());

            Question? added = null;
            _questions.Setup(r => r.AddAsync(It.IsAny<Question>()))
                      .Callback<Question>(q => added = q)
                      .Returns(Task.CompletedTask);

            var result = await CreateQuestionHandler().Handle(
                new CreateQuestionCommand(quiz.MaterialId, OwnerId, "1 + 1 = ?", QuestionType.SingleChoice),
                CancellationToken.None);

            Assert.True(result.Success);
            Assert.Empty(added!.AnswerOptions);   // câu hỏi không có đáp án nào — không ai chặn
        }

        // ============================================================ TC-015
        [Fact]
        [Trait("TC", "TC-UNIT-QuizService-015")]
        [Trait("UC", "UC-54")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task UpdateQuestion_Valid_KeepsOrderIndex()
        {
            var question = QuestionWithParents(OwnerId, orderIndex: 3);
            _questions.Setup(r => r.GetWithQuizAsync(question.Id, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(question);

            var result = await UpdateQuestionHandler().Handle(
                new UpdateQuestionCommand(question.Id, OwnerId, "Nội dung sửa", QuestionType.MultipleChoice),
                CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal("Nội dung sửa", question.Content);
            Assert.Equal(QuestionType.MultipleChoice, question.QuestionType);
            Assert.Equal(3, question.OrderIndex);   // vị trí giữ nguyên
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        [Trait("TC", "TC-UNIT-QuizService-016")]
        [Trait("UC", "UC-54")]
        [Trait("Type", "Security (GBR)")]
        [Trait("Priority", "High")]
        public async Task UpdateQuestion_ByNonOwner_Forbidden()
        {
            var question = QuestionWithParents(OwnerId);
            _questions.Setup(r => r.GetWithQuizAsync(question.Id, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(question);

            var result = await UpdateQuestionHandler().Handle(
                new UpdateQuestionCommand(question.Id, OtherExpertId, "Bị chiếm", QuestionType.SingleChoice),
                CancellationToken.None);

            Assert.Equal("FORBIDDEN", result.ErrorCode);
            Assert.Equal("1 + 1 = ?", question.Content);
            VerifyNotSaved();
        }

        // ============================================================ TC-016  ⚠ DEFECT
        // BR-02/BR-03 "single-choice đúng 1 đáp án đúng / multiple-choice ≥ 1" KHÔNG được
        // kiểm tra. Đổi từ MultipleChoice sang SingleChoice trên câu hỏi đang có 3 đáp án
        // đúng vẫn thành công → dữ liệu trở nên không nhất quán ngay lập tức.
        [Fact(Skip = "DEF-QZ-02 - Changing the question type does not revalidate the correct answers")]
        [Trait("TC", "TC-UNIT-QuizService-017")]
        [Trait("UC", "UC-54")]
        [Trait("BR", "BR-02")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        [Trait("Defect", "DEF-QZ-02")]
        public async Task UpdateQuestion_SwitchType_NoValidation()
        {
            var question = QuestionWithParents(OwnerId);
            _questions.Setup(r => r.GetWithQuizAsync(question.Id, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(question);

            var result = await UpdateQuestionHandler().Handle(
                new UpdateQuestionCommand(question.Id, OwnerId, "1 + 1 = ?", QuestionType.MultipleChoice),
                CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal(QuestionType.MultipleChoice, question.QuestionType);
            // Handler chỉ đụng Content + QuestionType; số đáp án đúng không hề được xét.
            Assert.Empty(question.AnswerOptions);
        }

        // ============================================================ TC-017  ⚠ DEFECT
        [Fact(Skip = "DEF-MAT-01 - Content can still be edited while the course is published")]
        [Trait("TC", "TC-UNIT-QuizService-017")]
        [Trait("UC", "UC-54")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        [Trait("Defect", "DEF-MAT-01")]
        public async Task UpdateQuestion_PublishedCourse_NoGuard()
        {
            var question = QuestionWithParents(OwnerId, coursePublished: true);
            _questions.Setup(r => r.GetWithQuizAsync(question.Id, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(question);

            var result = await UpdateQuestionHandler().Handle(
                new UpdateQuestionCommand(question.Id, OwnerId, "Sửa khi đang phát hành",
                                          QuestionType.SingleChoice),
                CancellationToken.None);

            Assert.True(question.QuizMaterial.Material.Module.Course.IsPublished);
            Assert.True(result.Success);
            Assert.Equal("Sửa khi đang phát hành", question.Content);
        }

        // ============================================================ TC-018  ⚠ DEFECT
        // Covers: BR-03. Khác DeleteLearningMaterial (CÓ reindex), DeleteQuestion chỉ xoá
        // rồi lưu — các câu hỏi còn lại giữ nguyên OrderIndex, sinh ra lỗ hổng số thứ tự.
        [Fact(Skip = "DEF-QZ-03 - DeleteQuestion does not reindex the remaining questions")]
        [Trait("TC", "TC-UNIT-QuizService-018")]
        [Trait("UC", "UC-55")]
        [Trait("BR", "BR-03")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        [Trait("Defect", "DEF-QZ-03")]
        public async Task DeleteQuestion_ByOwner_NoReindex()
        {
            var question = QuestionWithParents(OwnerId, orderIndex: 2);
            _questions.Setup(r => r.GetWithQuizAsync(question.Id, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(question);

            var result = await DeleteQuestionHandler().Handle(
                new DeleteQuestionCommand(question.Id, OwnerId), CancellationToken.None);

            Assert.True(result.Success);
            _questions.Verify(r => r.Delete(question), Times.Once);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            // Không hề nạp lại danh sách câu hỏi để đánh số lại — khác DeleteLearningMaterial.
            _questions.Verify(r => r.GetByQuizIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        [Trait("TC", "TC-UNIT-QuizService-019")]
        [Trait("UC", "UC-55")]
        [Trait("Type", "Security (GBR)")]
        [Trait("Priority", "High")]
        public async Task DeleteQuestion_ByNonOwner_Forbidden()
        {
            var question = QuestionWithParents(OwnerId);
            _questions.Setup(r => r.GetWithQuizAsync(question.Id, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(question);

            var result = await DeleteQuestionHandler().Handle(
                new DeleteQuestionCommand(question.Id, OtherExpertId), CancellationToken.None);

            Assert.Equal("FORBIDDEN", result.ErrorCode);
            _questions.Verify(r => r.Delete(It.IsAny<Question>()), Times.Never);
            VerifyNotSaved();
        }

        [Fact]
        [Trait("TC", "TC-UNIT-QuizService-020")]
        [Trait("UC", "UC-55")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        public async Task DeleteQuestion_NotFound_Rejected()
        {
            _questions.Setup(r => r.GetWithQuizAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync((Question?)null);

            var result = await DeleteQuestionHandler().Handle(
                new DeleteQuestionCommand(Guid.NewGuid(), OwnerId), CancellationToken.None);

            Assert.Equal("QUESTION_NOT_FOUND", result.ErrorCode);
            VerifyNotSaved();
        }

        // ============================================================ TC-019  ⚠ DEFECT
        [Fact(Skip = "DEF-MAT-02 - Content can still be deleted while the course is published")]
        [Trait("TC", "TC-UNIT-QuizService-020")]
        [Trait("UC", "UC-55")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        [Trait("Defect", "DEF-MAT-02")]
        public async Task DeleteQuestion_PublishedCourse_NoGuard()
        {
            var question = QuestionWithParents(OwnerId, coursePublished: true);
            _questions.Setup(r => r.GetWithQuizAsync(question.Id, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(question);

            var result = await DeleteQuestionHandler().Handle(
                new DeleteQuestionCommand(question.Id, OwnerId), CancellationToken.None);

            Assert.True(question.QuizMaterial.Material.Module.Course.IsPublished);
            Assert.True(result.Success);
            _questions.Verify(r => r.Delete(question), Times.Once);
        }
    }
}
