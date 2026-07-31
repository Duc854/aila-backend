using AILA.Application.Common.Interfaces;
using AILA.Application.Common.Interfaces.Repositories;
using AILA.Application.Features.AnswerOptions.Commands.CreateAnswerOption;
using AILA.Application.Features.AnswerOptions.Commands.DeleteAnswerOption;
using AILA.Application.Features.AnswerOptions.Commands.ReorderAnswerOptions;
using AILA.Application.Features.AnswerOptions.Commands.UpdateAnswerOption;
using AILA.Application.Features.AnswerOptions.Dtos;
using AILA.Application.Features.AnswerOptions.Queries.GetAnswerOptions;
using AILA.Application.Tests.Common;
using AILA.Application.Tests.Common.Builders;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using Moq;

namespace AILA.Application.Tests.Features.Quizzes
{
    /// <summary>
    /// Sheet: QuizService · UC-53 Create / UC-54 Update / UC-55 Remove / UC-56 Reorder question.
    /// TC-UNIT-QuizService-024 → 034.
    /// Quyền sở hữu đi qua chuỗi Question → QuizMaterial → Material → Module → Course → ExpertId,
    /// nên mọi test phải dựng đủ 5 tầng navigation; <see cref="TestEntity.SetProperty"/> mô phỏng
    /// phần EF thường tự gắn.
    /// </summary>
    public class AnswerOptionHandlerTests
    {
        private static readonly Guid OwnerId = Guid.NewGuid();
        private static readonly Guid OtherExpertId = Guid.NewGuid();

        private readonly Mock<IQuestionRepository> _questions = new();
        private readonly Mock<IAnswerOptionRepository> _answers = new();
        private readonly Mock<IUnitOfWork> _uow = new();

        public AnswerOptionHandlerTests()
        {
            _uow.Setup(u => u.Questions).Returns(_questions.Object);
            _uow.Setup(u => u.AnswerOptions).Returns(_answers.Object);
        }

        /// <summary>Question kèm đầy đủ navigation ngược lên Course.</summary>
        private static Question QuestionWithParents(
            Guid ownerId,
            QuestionType type = QuestionType.SingleChoice,
            int optionCount = 2,
            int[]? correctIndexes = null)
        {
            var course = new CourseBuilder().OwnedBy(ownerId).Build();
            var module = new Module(course.Id, "Chương mở đầu", 1);
            TestEntity.SetProperty(module, nameof(Module.Course), course);

            // Material chỉ dựng được qua factory; constructor là private.
            var material = Material.CreateQuiz(module.Id, "Bài kiểm tra số 1", 1);
            TestEntity.SetProperty(material, nameof(Material.Module), module);

            var quiz = new QuizMaterial(material.Id, 30, 70, true);
            TestEntity.SetProperty(quiz, nameof(QuizMaterial.Material), material);

            var question = new Question(material.Id, "Câu hỏi 1", type, 1);
            TestEntity.SetProperty(question, nameof(Question.QuizMaterial), quiz);

            correctIndexes ??= new[] { 0 };
            for (var i = 0; i < optionCount; i++)
                question.AddAnswerOption(
                    new AnswerOption(question.Id, $"Đáp án {i + 1}", correctIndexes.Contains(i), i + 1));

            return question;
        }

        private void QuestionReturns(Question? question, Guid? id = null)
        {
            var key = id ?? question?.Id ?? Guid.NewGuid();
            _questions.Setup(r => r.GetWithQuizAndAnswersAsync(key, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(question);
            _questions.Setup(r => r.GetWithQuizAsync(key, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(question);
        }

        // ============================================================ UC-53 Create option

        // ------------------------------------------------------------ TC-024
        // Covers: Main Flow. OrderIndex mới = max hiện có + 1, nên đáp án luôn nối vào cuối.
        [Fact]
        [Trait("TC", "TC-UNIT-QuizService-037")]
        [Trait("UC", "UC-53")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task CreateOption_AppendedAtEnd()
        {
            var question = QuestionWithParents(OwnerId, optionCount: 2, correctIndexes: new[] { 0 });
            QuestionReturns(question);

            var handler = new CreateAnswerOptionCommandHandler(_uow.Object);
            var result = await handler.Handle(
                new CreateAnswerOptionCommand(question.Id, OwnerId, "Đáp án C", false),
                CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal(3, question.AnswerOptions.Count);
            Assert.Equal(3, question.AnswerOptions.Max(o => o.OrderIndex));
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        // ------------------------------------------------------------ TC-025
        // Covers: BR-01 — không tìm thấy câu hỏi / không phải chủ khoá học.
        [Fact]
        [Trait("TC", "TC-UNIT-QuizService-038")]
        [Trait("UC", "UC-53")]
        [Trait("BR", "BR-01")]
        [Trait("Type", "Security (GBR)")]
        [Trait("Priority", "Critical")]
        public async Task CreateOption_MissingOrForeign_Rejected()
        {
            var handler = new CreateAnswerOptionCommandHandler(_uow.Object);

            var unknownId = Guid.NewGuid();
            QuestionReturns(null, unknownId);
            var missing = await handler.Handle(
                new CreateAnswerOptionCommand(unknownId, OwnerId, "X", false), CancellationToken.None);
            Assert.False(missing.Success);
            Assert.Equal("QUESTION_NOT_FOUND", missing.ErrorCode);

            var foreign = QuestionWithParents(OtherExpertId);
            QuestionReturns(foreign);
            var denied = await handler.Handle(
                new CreateAnswerOptionCommand(foreign.Id, OwnerId, "X", false), CancellationToken.None);
            Assert.False(denied.Success);
            Assert.Equal("FORBIDDEN", denied.ErrorCode);

            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        // ------------------------------------------------------------ TC-026
        // Covers: BR-02 — câu hỏi SingleChoice phải có ĐÚNG một đáp án đúng. Thêm đáp án
        // đúng thứ hai làm câu hỏi không chấm được, phải bị chặn.
        [Fact]
        [Trait("TC", "TC-UNIT-QuizService-039")]
        [Trait("UC", "UC-53")]
        [Trait("BR", "BR-02")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task CreateOption_SecondCorrectOnSingleChoice_Rejected()
        {
            var question = QuestionWithParents(OwnerId, QuestionType.SingleChoice,
                optionCount: 2, correctIndexes: new[] { 0 });
            QuestionReturns(question);

            var handler = new CreateAnswerOptionCommandHandler(_uow.Object);
            var result = await handler.Handle(
                new CreateAnswerOptionCommand(question.Id, OwnerId, "Cũng đúng", true),
                CancellationToken.None);

            Assert.False(result.Success);
            Assert.Equal("INVALID_ANSWER_OPTIONS", result.ErrorCode);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        // ------------------------------------------------------------ TC-027
        // Covers: BR-02 với MultipleChoice — nhiều đáp án đúng là hợp lệ.
        [Fact]
        [Trait("TC", "TC-UNIT-QuizService-039")]
        [Trait("UC", "UC-53")]
        [Trait("BR", "BR-02")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        public async Task CreateOption_SecondCorrectOnMultipleChoice_Allowed()
        {
            var question = QuestionWithParents(OwnerId, QuestionType.MultipleChoice,
                optionCount: 2, correctIndexes: new[] { 0 });
            QuestionReturns(question);

            var handler = new CreateAnswerOptionCommandHandler(_uow.Object);
            var result = await handler.Handle(
                new CreateAnswerOptionCommand(question.Id, OwnerId, "Cũng đúng", true),
                CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal(2, question.AnswerOptions.Count(o => o.IsCorrect));
        }

        // ============================================================ UC-54 Update option

        // ------------------------------------------------------------ TC-028
        [Fact]
        [Trait("TC", "TC-UNIT-QuizService-040")]
        [Trait("UC", "UC-54")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task UpdateOption_ContentChanged()
        {
            var question = QuestionWithParents(OwnerId, optionCount: 2, correctIndexes: new[] { 0 });
            var target = question.AnswerOptions.First(o => !o.IsCorrect);
            TestEntity.SetProperty(target, nameof(AnswerOption.Question), question);

            _answers.Setup(r => r.GetWithQuestionAsync(target.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(target);
            QuestionReturns(question, target.QuestionId);

            var handler = new UpdateAnswerOptionCommandHandler(_uow.Object);
            var result = await handler.Handle(
                new UpdateAnswerOptionCommand(target.Id, OwnerId, "Nội dung đã sửa", false),
                CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal("Nội dung đã sửa", target.Content);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        // ------------------------------------------------------------ TC-029
        // Covers: BR-01 — không tìm thấy / không phải chủ sở hữu.
        [Fact]
        [Trait("TC", "TC-UNIT-QuizService-041")]
        [Trait("UC", "UC-54")]
        [Trait("BR", "BR-01")]
        [Trait("Type", "Security (GBR)")]
        [Trait("Priority", "Critical")]
        public async Task UpdateOption_MissingOrForeign_Rejected()
        {
            var handler = new UpdateAnswerOptionCommandHandler(_uow.Object);

            _answers.Setup(r => r.GetWithQuestionAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((AnswerOption?)null);
            var missing = await handler.Handle(
                new UpdateAnswerOptionCommand(Guid.NewGuid(), OwnerId, "X", false), CancellationToken.None);
            Assert.Equal("ANSWER_NOT_FOUND", missing.ErrorCode);

            var foreignQuestion = QuestionWithParents(OtherExpertId, optionCount: 2);
            var foreignOption = foreignQuestion.AnswerOptions.First();
            TestEntity.SetProperty(foreignOption, nameof(AnswerOption.Question), foreignQuestion);
            _answers.Setup(r => r.GetWithQuestionAsync(foreignOption.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(foreignOption);

            var denied = await handler.Handle(
                new UpdateAnswerOptionCommand(foreignOption.Id, OwnerId, "X", false), CancellationToken.None);
            Assert.Equal("FORBIDDEN", denied.ErrorCode);

            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        // ------------------------------------------------------------ TC-030
        // Covers: BR-02 — bỏ cờ đúng của đáp án đúng DUY NHẤT làm câu hỏi không còn chấm được.
        [Fact]
        [Trait("TC", "TC-UNIT-QuizService-041")]
        [Trait("UC", "UC-54")]
        [Trait("BR", "BR-02")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task UpdateOption_ClearingOnlyCorrect_Rejected()
        {
            var question = QuestionWithParents(OwnerId, optionCount: 2, correctIndexes: new[] { 0 });
            var onlyCorrect = question.AnswerOptions.First(o => o.IsCorrect);
            TestEntity.SetProperty(onlyCorrect, nameof(AnswerOption.Question), question);

            _answers.Setup(r => r.GetWithQuestionAsync(onlyCorrect.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(onlyCorrect);
            QuestionReturns(question, onlyCorrect.QuestionId);

            var handler = new UpdateAnswerOptionCommandHandler(_uow.Object);
            var result = await handler.Handle(
                new UpdateAnswerOptionCommand(onlyCorrect.Id, OwnerId, "Đáp án 1", false),
                CancellationToken.None);

            Assert.False(result.Success);
            Assert.Equal("INVALID_ANSWER_OPTIONS", result.ErrorCode);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        // ============================================================ UC-55 Remove option

        // ------------------------------------------------------------ TC-031
        [Fact]
        [Trait("TC", "TC-UNIT-QuizService-042")]
        [Trait("UC", "UC-55")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task DeleteOption_SurplusRemoved()
        {
            var question = QuestionWithParents(OwnerId, optionCount: 4, correctIndexes: new[] { 0 });
            var surplus = question.AnswerOptions.Last();
            TestEntity.SetProperty(surplus, nameof(AnswerOption.Question), question);

            _answers.Setup(r => r.GetWithQuestionAsync(surplus.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(surplus);
            QuestionReturns(question, surplus.QuestionId);

            var handler = new DeleteAnswerOptionCommandHandler(_uow.Object);
            var result = await handler.Handle(
                new DeleteAnswerOptionCommand(surplus.Id, OwnerId), CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal(3, question.AnswerOptions.Count);
            _answers.Verify(r => r.Delete(surplus), Times.Once);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        // ------------------------------------------------------------ TC-032
        // Covers: BR-01 — xoá đáp án đúng duy nhất khiến câu hỏi không chấm được nữa,
        // đây là biên giữ cho bài kiểm tra còn dùng được.
        [Fact]
        [Trait("TC", "TC-UNIT-QuizService-043")]
        [Trait("UC", "UC-55")]
        [Trait("BR", "BR-01")]
        [Trait("Type", "Boundary & Negative")]
        [Trait("Priority", "Critical")]
        public async Task DeleteOption_OnlyCorrect_Rejected()
        {
            var question = QuestionWithParents(OwnerId, optionCount: 3, correctIndexes: new[] { 0 });
            var onlyCorrect = question.AnswerOptions.First(o => o.IsCorrect);
            TestEntity.SetProperty(onlyCorrect, nameof(AnswerOption.Question), question);

            _answers.Setup(r => r.GetWithQuestionAsync(onlyCorrect.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(onlyCorrect);
            QuestionReturns(question, onlyCorrect.QuestionId);

            var handler = new DeleteAnswerOptionCommandHandler(_uow.Object);
            var result = await handler.Handle(
                new DeleteAnswerOptionCommand(onlyCorrect.Id, OwnerId), CancellationToken.None);

            Assert.False(result.Success);
            Assert.Equal("INVALID_ANSWER_OPTIONS", result.ErrorCode);
            _answers.Verify(r => r.Delete(It.IsAny<AnswerOption>()), Times.Never);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        // ------------------------------------------------------------ TC-033
        [Fact]
        [Trait("TC", "TC-UNIT-QuizService-043")]
        [Trait("UC", "UC-55")]
        [Trait("Type", "Security (GBR)")]
        [Trait("Priority", "Critical")]
        public async Task DeleteOption_MissingOrForeign_Rejected()
        {
            var handler = new DeleteAnswerOptionCommandHandler(_uow.Object);

            _answers.Setup(r => r.GetWithQuestionAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((AnswerOption?)null);
            var missing = await handler.Handle(
                new DeleteAnswerOptionCommand(Guid.NewGuid(), OwnerId), CancellationToken.None);
            Assert.Equal("ANSWER_NOT_FOUND", missing.ErrorCode);

            var foreignQuestion = QuestionWithParents(OtherExpertId, optionCount: 3);
            var foreignOption = foreignQuestion.AnswerOptions.Last();
            TestEntity.SetProperty(foreignOption, nameof(AnswerOption.Question), foreignQuestion);
            _answers.Setup(r => r.GetWithQuestionAsync(foreignOption.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(foreignOption);

            var denied = await handler.Handle(
                new DeleteAnswerOptionCommand(foreignOption.Id, OwnerId), CancellationToken.None);
            Assert.Equal("FORBIDDEN", denied.ErrorCode);

            _answers.Verify(r => r.Delete(It.IsAny<AnswerOption>()), Times.Never);
        }

        // ============================================================ UC-56 Reorder

        // ------------------------------------------------------------ TC-034
        [Fact]
        [Trait("TC", "TC-UNIT-QuizService-044")]
        [Trait("UC", "UC-56")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        public async Task ReorderOptions_AppliesNewOrder()
        {
            var question = QuestionWithParents(OwnerId, optionCount: 3, correctIndexes: new[] { 0 });
            QuestionReturns(question);

            var options = question.AnswerOptions.ToList();
            _answers.Setup(r => r.GetByQuestionIdAsync(question.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(options);

            var handler = new ReorderAnswerOptionsCommandHandler(_uow.Object);
            var result = await handler.Handle(
                new ReorderAnswerOptionsCommand(question.Id, OwnerId, new List<AnswerOptionOrderItem>
                {
                    new() { AnswerOptionId = options[2].Id, NewOrderIndex = 1 },
                    new() { AnswerOptionId = options[0].Id, NewOrderIndex = 2 },
                    new() { AnswerOptionId = options[1].Id, NewOrderIndex = 3 },
                }),
                CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal(1, options[2].OrderIndex);
            Assert.Equal(2, options[0].OrderIndex);
            Assert.Equal(3, options[1].OrderIndex);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        // ------------------------------------------------------------ TC-035
        // Covers: BR-01 — id lạ trong danh sách bị bỏ qua im lặng (map.TryGetValue).
        // Ghi nhận hành vi hiện tại: client gửi danh sách cũ/thiếu sẽ KHÔNG bị báo lỗi.
        [Fact]
        [Trait("TC", "TC-UNIT-QuizService-045")]
        [Trait("UC", "UC-56")]
        [Trait("BR", "BR-01")]
        [Trait("Type", "Boundary & Negative")]
        [Trait("Priority", "Medium")]
        public async Task ReorderOptions_UnknownIdIgnoredSilently()
        {
            var question = QuestionWithParents(OwnerId, optionCount: 2, correctIndexes: new[] { 0 });
            QuestionReturns(question);

            var options = question.AnswerOptions.ToList();
            _answers.Setup(r => r.GetByQuestionIdAsync(question.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(options);

            var handler = new ReorderAnswerOptionsCommandHandler(_uow.Object);
            var result = await handler.Handle(
                new ReorderAnswerOptionsCommand(question.Id, OwnerId, new List<AnswerOptionOrderItem>
                {
                    new() { AnswerOptionId = Guid.NewGuid(), NewOrderIndex = 1 },      // id không thuộc câu hỏi này
                    new() { AnswerOptionId = options[0].Id, NewOrderIndex = 2 },
                }),
                CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal(2, options[0].OrderIndex);
            Assert.Equal(2, options[1].OrderIndex);   // không nằm trong danh sách gửi lên → giữ nguyên
        }

        // ------------------------------------------------------------ TC-036
        [Fact]
        [Trait("TC", "TC-UNIT-QuizService-045")]
        [Trait("UC", "UC-56")]
        [Trait("Type", "Security (GBR)")]
        [Trait("Priority", "Critical")]
        public async Task ReorderOptions_MissingOrForeign_Rejected()
        {
            var handler = new ReorderAnswerOptionsCommandHandler(_uow.Object);
            var empty = new List<AnswerOptionOrderItem>();

            var unknownId = Guid.NewGuid();
            QuestionReturns(null, unknownId);
            var missing = await handler.Handle(
                new ReorderAnswerOptionsCommand(unknownId, OwnerId, empty), CancellationToken.None);
            Assert.Equal("QUESTION_NOT_FOUND", missing.ErrorCode);

            var foreign = QuestionWithParents(OtherExpertId);
            QuestionReturns(foreign);
            var denied = await handler.Handle(
                new ReorderAnswerOptionsCommand(foreign.Id, OwnerId, empty), CancellationToken.None);
            Assert.Equal("FORBIDDEN", denied.ErrorCode);

            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        // ============================================================ UC-53 Read options

        // ------------------------------------------------------------ TC-037
        [Fact]
        [Trait("TC", "TC-UNIT-QuizService-046")]
        [Trait("UC", "UC-53")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        public async Task GetOptions_ReturnsOptionsWithCorrectFlag()
        {
            var question = QuestionWithParents(OwnerId, optionCount: 3, correctIndexes: new[] { 1 });
            QuestionReturns(question);
            _answers.Setup(r => r.GetByQuestionIdAsync(question.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(question.AnswerOptions.ToList());

            var handler = new GetAnswerOptionsQueryHandler(_uow.Object);
            var result = await handler.Handle(
                new GetAnswerOptionsQuery(question.Id, OwnerId), CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal(3, result.Data!.Count);
            Assert.Single(result.Data, o => o.IsCorrect);
        }

        // ------------------------------------------------------------ TC-038
        // Covers: BR-03 — lộ cờ IsCorrect cho người không sở hữu là lộ đáp án của cả bài thi.
        [Fact]
        [Trait("TC", "TC-UNIT-QuizService-047")]
        [Trait("UC", "UC-53")]
        [Trait("BR", "BR-03")]
        [Trait("Type", "Security (GBR)")]
        [Trait("Priority", "Critical")]
        public async Task GetOptions_MissingOrForeign_NoAnswerLeak()
        {
            var handler = new GetAnswerOptionsQueryHandler(_uow.Object);

            var unknownId = Guid.NewGuid();
            QuestionReturns(null, unknownId);
            var missing = await handler.Handle(
                new GetAnswerOptionsQuery(unknownId, OwnerId), CancellationToken.None);
            Assert.Equal("QUESTION_NOT_FOUND", missing.ErrorCode);
            Assert.Null(missing.Data);

            var foreign = QuestionWithParents(OtherExpertId, optionCount: 3, correctIndexes: new[] { 1 });
            QuestionReturns(foreign);
            var denied = await handler.Handle(
                new GetAnswerOptionsQuery(foreign.Id, OwnerId), CancellationToken.None);
            Assert.Equal("FORBIDDEN", denied.ErrorCode);
            Assert.Null(denied.Data);

            _answers.Verify(r => r.GetByQuestionIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
