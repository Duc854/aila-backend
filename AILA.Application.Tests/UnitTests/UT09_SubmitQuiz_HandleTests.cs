using AILA.Application.Common.Interfaces;
using AILA.Application.Common.Interfaces.Repositories;
using AILA.Application.Features.Quizzes.Commands.SubmitQuiz;
using AILA.Application.Features.Quizzes.Dtos;
using AILA.Application.Tests.UnitTests.TestHelpers;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;

namespace AILA.Application.Tests.UnitTests;

/// <summary>
/// Sheet: UT09_SubmitQuiz — <see cref="SubmitQuizCommandHandler.Handle"/>
/// Module: Quiz · CC = 22 · 24 test case (mỗi đường thực thi độc lập 1 TC — spec §3.3)
///
/// Nhánh: B1 enrollment null · B2 attempt null · B3/B4 sai chủ sở hữu / sai quiz
///        B5 quiz null · B6/B7 quiz chưa cấu hình · B8 đã nộp (idempotent)
///        B10 vòng lặp answers · B11 câu hỏi lạ · B12 SelectedOptionIds null · B13 đáp án lạ
///        B14 auto-submit · B15/B16 vòng lặp ghi đáp án · B17 đạt/trượt
///        B18 cập nhật tiến độ · B19 tạo progress mới · B20 idempotent tiến độ · B21 rollback
/// </summary>
public class UT09_SubmitQuiz_HandleTests
{
    private static readonly Guid CourseId = Guid.NewGuid();
    private static readonly Guid MaterialId = Guid.NewGuid();
    private static readonly Guid LearnerId = Guid.NewGuid();

    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IEnrollmentRepository> _enrollments = new();
    private readonly Mock<IQuizRepository> _quizzes = new();
    private readonly Mock<ILearningProgressRepository> _progresses = new();

    private readonly Enrollment _enrollment = new(LearnerId, CourseId, totalMaterials: 3);

    public UT09_SubmitQuiz_HandleTests()
    {
        _uow.SetupGet(x => x.Enrollments).Returns(_enrollments.Object);
        _uow.SetupGet(x => x.Quizzes).Returns(_quizzes.Object);
        _uow.SetupGet(x => x.LearningProgresses).Returns(_progresses.Object);

        _enrollments.Setup(x => x.GetByCourseAndLearnerAsync(CourseId, LearnerId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(_enrollment);
        _progresses.Setup(x => x.GetByCompositeKeyAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync((LearningProgress?)null);
    }

    private SubmitQuizCommandHandler CreateSut() =>
        new(_uow.Object, Mock.Of<ILogger<SubmitQuizCommandHandler>>());

    // ---------- Fixture builders ----------

    /// <summary>Quiz 3 câu, mỗi câu 2 đáp án: index 0 đúng, index 1 sai.</summary>
    private static QuizMaterial BuildQuiz(int questionCount = 3, decimal passingScore = 60m,
        int timeLimitMinutes = 30, bool withCorrectOption = true)
    {
        var quiz = new QuizMaterial(MaterialId, timeLimitMinutes, passingScore, true);

        for (var i = 0; i < questionCount; i++)
        {
            var question = new Question(MaterialId, $"Câu hỏi {i + 1}", QuestionType.SingleChoice, i + 1);
            question.AddAnswerOption(new AnswerOption(question.Id, "Đáp án A", withCorrectOption, 1));
            question.AddAnswerOption(new AnswerOption(question.Id, "Đáp án B", false, 2));
            quiz.AddQuestion(question);
        }

        return quiz;
    }

    private static Question QuestionAt(QuizMaterial quiz, int index) => quiz.Questions.ElementAt(index);

    private static Guid CorrectOptionOf(Question question) =>
        question.AnswerOptions.First(o => o.IsCorrect).Id;

    private static Guid WrongOptionOf(Question question) =>
        question.AnswerOptions.First(o => !o.IsCorrect).Id;

    /// <summary>Bài nộp chọn đúng <paramref name="correctCount"/> câu đầu, các câu còn lại chọn sai.</summary>
    private static List<QuizAnswerSubmissionDto> BuildAnswers(QuizMaterial quiz, int correctCount)
    {
        return quiz.Questions.Select((q, i) => new QuizAnswerSubmissionDto
        {
            QuestionId = q.Id,
            SelectedOptionIds = new List<Guid> { i < correctCount ? CorrectOptionOf(q) : WrongOptionOf(q) }
        }).ToList();
    }

    private QuizAttempt SetupScenario(QuizMaterial? quiz, QuizAttempt? attempt)
    {
        _quizzes.Setup(x => x.GetQuizForLearningAsync(CourseId, MaterialId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(quiz);
        _quizzes.Setup(x => x.GetAttemptWithAnswersAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(attempt);
        return attempt!;
    }

    private QuizAttempt NewAttempt() => new(_enrollment.Id, MaterialId);

    private Task<Shared.Wrappers.ResponseDto<QuizResultDto>> Act(
        IReadOnlyList<QuizAnswerSubmissionDto>? answers = null, Guid? attemptId = null) =>
        CreateSut().Handle(
            new SubmitQuizCommand(CourseId, MaterialId, attemptId ?? Guid.NewGuid(), LearnerId,
                answers ?? new List<QuizAnswerSubmissionDto>()),
            CancellationToken.None);

    // ---------- Test cases ----------

    /// <summary>UTCID01 · B1=T · Type A — chưa đăng ký khoá học.</summary>
    [Fact]
    public async Task UTCID01_EnrollmentNotFound_ReturnsEnrollmentNotFound()
    {
        _enrollments.Setup(x => x.GetByCourseAndLearnerAsync(CourseId, LearnerId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync((Enrollment?)null);

        var result = await Act();

        Assert.False(result.Success);
        Assert.Equal("ENROLLMENT_NOT_FOUND", result.ErrorCode);
        _uow.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>UTCID02 · B2=T · Type A — không tìm thấy lượt làm bài.</summary>
    [Fact]
    public async Task UTCID02_AttemptNotFound_ReturnsAttemptNotFound()
    {
        SetupScenario(BuildQuiz(), null);

        var result = await Act();

        Assert.False(result.Success);
        Assert.Equal("ATTEMPT_NOT_FOUND", result.ErrorCode);
        _uow.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>UTCID03 · B3=T · Type A — lượt làm bài của Enrollment khác (không phải chính chủ).</summary>
    [Fact]
    public async Task UTCID03_AttemptBelongsToAnotherEnrollment_ReturnsForbidden()
    {
        SetupScenario(BuildQuiz(), new QuizAttempt(Guid.NewGuid(), MaterialId));

        var result = await Act();

        Assert.False(result.Success);
        Assert.Equal("FORBIDDEN", result.ErrorCode);
    }

    /// <summary>UTCID04 · B3=F, B4=T · Type A — lượt làm bài thuộc bài kiểm tra khác.</summary>
    [Fact]
    public async Task UTCID04_AttemptBelongsToAnotherQuiz_ReturnsForbidden()
    {
        SetupScenario(BuildQuiz(), new QuizAttempt(_enrollment.Id, Guid.NewGuid()));

        var result = await Act();

        Assert.False(result.Success);
        Assert.Equal("FORBIDDEN", result.ErrorCode);
    }

    /// <summary>UTCID05 · B5=T · Type A — không tìm thấy bài kiểm tra trong khoá học.</summary>
    [Fact]
    public async Task UTCID05_QuizNotFound_ReturnsQuizNotFound()
    {
        SetupScenario(null, NewAttempt());

        var result = await Act();

        Assert.False(result.Success);
        Assert.Equal("QUIZ_NOT_FOUND", result.ErrorCode);
    }

    /// <summary>UTCID06 · B6=T · Type B — bài kiểm tra không có câu hỏi nào (tập rỗng).</summary>
    [Fact]
    public async Task UTCID06_QuizWithoutQuestions_ReturnsQuizNotConfigured()
    {
        SetupScenario(BuildQuiz(questionCount: 0), NewAttempt());

        var result = await Act();

        Assert.False(result.Success);
        Assert.Equal("QUIZ_NOT_CONFIGURED", result.ErrorCode);
        _uow.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>UTCID07 · B6=F, B7=T · Type A — có câu hỏi nhưng không câu nào có đáp án đúng.</summary>
    [Fact]
    public async Task UTCID07_QuizWithoutCorrectOption_ReturnsQuizNotConfigured()
    {
        SetupScenario(BuildQuiz(questionCount: 1, withCorrectOption: false), NewAttempt());

        var result = await Act();

        Assert.False(result.Success);
        Assert.Equal("QUIZ_NOT_CONFIGURED", result.ErrorCode);
    }

    /// <summary>
    /// UTCID08 · B8=T, B9=T · Type N — bài đã nộp trước đó (double-submit).
    /// Idempotent: trả lại kết quả cũ, KHÔNG mở transaction, KHÔNG ghi thêm đáp án.
    /// </summary>
    [Fact]
    public async Task UTCID08_AlreadySubmitted_ReturnsPreviousResultWithoutRegrading()
    {
        var quiz = BuildQuiz();
        var attempt = NewAttempt();

        attempt.AddAnswer(new QuizAnswer(attempt.Id, QuestionAt(quiz, 0).Id, CorrectOptionOf(QuestionAt(quiz, 0))));
        attempt.AddAnswer(new QuizAnswer(attempt.Id, QuestionAt(quiz, 1).Id, CorrectOptionOf(QuestionAt(quiz, 1))));
        attempt.Submit(66.67m, isPassed: true);

        SetupScenario(quiz, attempt);

        var result = await Act(BuildAnswers(quiz, correctCount: 3));

        Assert.True(result.Success);
        Assert.Equal(2, result.Data!.CorrectAnswers);
        Assert.Equal(66.67m, result.Data.Score);
        Assert.False(result.Data.WasAutoSubmitted);
        _uow.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _quizzes.Verify(x => x.AddAnswerAsync(It.IsAny<QuizAnswer>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>UTCID09 · B8=T, B9=F · Type B — bài đã nộp nhưng không có câu trả lời nào.</summary>
    [Fact]
    public async Task UTCID09_AlreadySubmittedWithNoAnswers_ReturnsZeroCorrect()
    {
        var quiz = BuildQuiz();
        var attempt = NewAttempt();
        attempt.Submit(0m, isPassed: false);

        SetupScenario(quiz, attempt);

        var result = await Act();

        Assert.True(result.Success);
        Assert.Equal(0, result.Data!.CorrectAnswers);
        Assert.Equal(0m, result.Data.Score);
    }

    /// <summary>UTCID10 · B10=1 vòng, B11=T · Type A — bài nộp chứa câu hỏi không thuộc quiz.</summary>
    [Fact]
    public async Task UTCID10_AnswerWithUnknownQuestion_ReturnsInvalidSubmission()
    {
        var quiz = BuildQuiz();
        SetupScenario(quiz, NewAttempt());

        var result = await Act(new List<QuizAnswerSubmissionDto>
        {
            new() { QuestionId = Guid.NewGuid(), SelectedOptionIds = new List<Guid> { Guid.NewGuid() } }
        });

        Assert.False(result.Success);
        Assert.Equal("INVALID_SUBMISSION", result.ErrorCode);
        Assert.Equal("Bài nộp chứa câu hỏi không thuộc bài kiểm tra này.", result.ErrorMessage);
        _uow.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>UTCID11 · B11=F, B13=T · Type A — bài nộp chứa đáp án không thuộc câu hỏi.</summary>
    [Fact]
    public async Task UTCID11_AnswerWithUnknownOption_ReturnsInvalidSubmission()
    {
        var quiz = BuildQuiz();
        SetupScenario(quiz, NewAttempt());

        var result = await Act(new List<QuizAnswerSubmissionDto>
        {
            new() { QuestionId = QuestionAt(quiz, 0).Id, SelectedOptionIds = new List<Guid> { Guid.NewGuid() } }
        });

        Assert.False(result.Success);
        Assert.Equal("INVALID_SUBMISSION", result.ErrorCode);
        Assert.Equal("Bài nộp chứa đáp án không hợp lệ cho câu hỏi.", result.ErrorMessage);
    }

    /// <summary>UTCID12 · B12 (toán tử ??), B13=F · Type B — SelectedOptionIds null ⇒ coi như bỏ trống.</summary>
    [Fact]
    public async Task UTCID12_NullSelectedOptionIds_TreatedAsEmptyAndScoresZero()
    {
        var quiz = BuildQuiz();
        SetupScenario(quiz, NewAttempt());

        var result = await Act(new List<QuizAnswerSubmissionDto>
        {
            new() { QuestionId = QuestionAt(quiz, 0).Id, SelectedOptionIds = null! }
        });

        Assert.True(result.Success);
        Assert.Equal(0m, result.Data!.Score);
        Assert.Equal(0, result.Data.CorrectAnswers);
    }

    /// <summary>UTCID13 · B10 = 0 vòng lặp · Type B — nộp bài trống.</summary>
    [Fact]
    public async Task UTCID13_EmptyAnswerList_ScoresZeroAndFails()
    {
        var quiz = BuildQuiz();
        SetupScenario(quiz, NewAttempt());

        var result = await Act(new List<QuizAnswerSubmissionDto>());

        Assert.True(result.Success);
        Assert.Equal(0m, result.Data!.Score);
        Assert.False(result.Data.IsPassed);
        _quizzes.Verify(x => x.AddAnswerAsync(It.IsAny<QuizAnswer>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// UTCID14 · B17=F, B18=F · Type B — 1/3 câu đúng ⇒ 33.33 &lt; 60 ⇒ TRƯỢT.
    /// Bất biến: trượt thì attempt vẫn Submitted nhưng KHÔNG cộng tiến độ.
    /// </summary>
    [Fact]
    public async Task UTCID14_FailedQuiz_DoesNotUpdateProgress()
    {
        var quiz = BuildQuiz();
        var attempt = SetupScenario(quiz, NewAttempt());

        var result = await Act(BuildAnswers(quiz, correctCount: 1));

        Assert.True(result.Success);
        Assert.Equal(33.33m, result.Data!.Score);
        Assert.False(result.Data.IsPassed);
        Assert.Equal(QuizAttemptStatus.Submitted, attempt.Status);
        Assert.Equal(0.00m, _enrollment.ProgressPct);
        _enrollments.Verify(x => x.Update(It.IsAny<Enrollment>()), Times.Never);
        _progresses.Verify(x => x.AddAsync(It.IsAny<LearningProgress>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>UTCID15 · B17=T, B18=T · Type N — 2/3 câu đúng ⇒ 66.67 ≥ 60 ⇒ ĐẠT.</summary>
    [Fact]
    public async Task UTCID15_PassedQuiz_UpdatesProgressAndCommits()
    {
        var quiz = BuildQuiz();
        SetupScenario(quiz, NewAttempt());

        var result = await Act(BuildAnswers(quiz, correctCount: 2));

        Assert.True(result.Success);
        Assert.Equal(66.67m, result.Data!.Score);
        Assert.True(result.Data.IsPassed);
        Assert.Equal(2, result.Data.CorrectAnswers);
        Assert.Equal(3, result.Data.TotalQuestions);
        _uow.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>UTCID16 · B17=T · Type B — 3/3 câu đúng ⇒ 100.00 (biên trên).</summary>
    [Fact]
    public async Task UTCID16_AllCorrect_ScoresOneHundred()
    {
        var quiz = BuildQuiz();
        SetupScenario(quiz, NewAttempt());

        var result = await Act(BuildAnswers(quiz, correctCount: 3));

        Assert.True(result.Success);
        Assert.Equal(100.00m, result.Data!.Score);
        Assert.True(result.Data.IsPassed);
    }

    /// <summary>UTCID17 · B17=T · Type B — điểm BẰNG ĐÚNG PassingScore ⇒ vẫn ĐẠT (toán tử &gt;=).</summary>
    [Fact]
    public async Task UTCID17_ScoreExactlyEqualsPassingScore_IsPassed()
    {
        var quiz = BuildQuiz(passingScore: 66.67m);
        SetupScenario(quiz, NewAttempt());

        var result = await Act(BuildAnswers(quiz, correctCount: 2));

        Assert.Equal(66.67m, result.Data!.Score);
        Assert.True(result.Data.IsPassed);
    }

    /// <summary>UTCID18 · B17=F · Type B — điểm thấp hơn PassingScore đúng 0.01 ⇒ TRƯỢT.</summary>
    [Fact]
    public async Task UTCID18_ScoreJustBelowPassingScore_IsNotPassed()
    {
        var quiz = BuildQuiz(passingScore: 66.68m);
        SetupScenario(quiz, NewAttempt());

        var result = await Act(BuildAnswers(quiz, correctCount: 2));

        Assert.Equal(66.67m, result.Data!.Score);
        Assert.False(result.Data.IsPassed);
    }

    /// <summary>UTCID19 · B18=T, B19=T · Type N — chưa có LearningProgress ⇒ tạo mới và cộng tiến độ.</summary>
    [Fact]
    public async Task UTCID19_PassedWithoutExistingProgress_CreatesProgressAndIncrementsEnrollment()
    {
        var quiz = BuildQuiz();
        SetupScenario(quiz, NewAttempt());

        var result = await Act(BuildAnswers(quiz, correctCount: 2));

        Assert.True(result.Success);
        _progresses.Verify(x => x.AddAsync(It.IsAny<LearningProgress>(), It.IsAny<CancellationToken>()), Times.Once);
        _enrollments.Verify(x => x.Update(_enrollment), Times.Once);
        Assert.Equal(1, _enrollment.CompletedMaterials);
        Assert.Equal(33.33m, _enrollment.ProgressPct);
        Assert.Equal(33.33m, result.Data!.ProgressPct);
    }

    /// <summary>UTCID20 · B19=F, B20=T · Type N — đã có LearningProgress nhưng chưa hoàn thành.</summary>
    [Fact]
    public async Task UTCID20_PassedWithIncompleteProgress_CompletesItAndIncrementsEnrollment()
    {
        var quiz = BuildQuiz();
        SetupScenario(quiz, NewAttempt());

        var progress = new LearningProgress(_enrollment.Id, MaterialId);
        _progresses.Setup(x => x.GetByCompositeKeyAsync(_enrollment.Id, MaterialId, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(progress);

        var result = await Act(BuildAnswers(quiz, correctCount: 2));

        Assert.True(result.Success);
        Assert.True(progress.IsCompleted);
        _progresses.Verify(x => x.AddAsync(It.IsAny<LearningProgress>(), It.IsAny<CancellationToken>()), Times.Never);
        _enrollments.Verify(x => x.Update(_enrollment), Times.Once);
        Assert.Equal(1, _enrollment.CompletedMaterials);
    }

    /// <summary>
    /// UTCID21 · B20=F · Type A — học liệu ĐÃ hoàn thành từ trước, làm lại vẫn đạt.
    /// Idempotent: KHÔNG cộng tiến độ lần hai.
    /// </summary>
    [Fact]
    public async Task UTCID21_PassedWithAlreadyCompletedProgress_DoesNotIncrementProgressAgain()
    {
        var quiz = BuildQuiz();
        SetupScenario(quiz, NewAttempt());

        var progress = new LearningProgress(_enrollment.Id, MaterialId);
        progress.Complete();
        _progresses.Setup(x => x.GetByCompositeKeyAsync(_enrollment.Id, MaterialId, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(progress);

        var result = await Act(BuildAnswers(quiz, correctCount: 2));

        Assert.True(result.Success);
        Assert.True(result.Data!.IsPassed);
        _enrollments.Verify(x => x.Update(It.IsAny<Enrollment>()), Times.Never);
        Assert.Equal(0, _enrollment.CompletedMaterials);
        Assert.Equal(0.00m, _enrollment.ProgressPct);
    }

    /// <summary>
    /// UTCID22 · B14=T · Type B — quá hạn nộp bài.
    /// StartedAt lùi 60 phút (reflection) trong khi TimeLimitMinutes = 30 ⇒ auto-submit.
    /// </summary>
    [Fact]
    public async Task UTCID22_SubmittedAfterDeadline_MarksAsAutoSubmitted()
    {
        var quiz = BuildQuiz(timeLimitMinutes: 30);
        var attempt = NewAttempt();
        PrivateSetter.Set(attempt, nameof(QuizAttempt.StartedAt), DateTime.UtcNow.AddMinutes(-60));
        SetupScenario(quiz, attempt);

        var result = await Act(BuildAnswers(quiz, correctCount: 2));

        Assert.True(result.Success);
        Assert.True(result.Data!.WasAutoSubmitted);
    }

    /// <summary>UTCID23 · B21=T · Type A — commit thất bại ⇒ rollback và ném lại exception.</summary>
    [Fact]
    public async Task UTCID23_CommitFails_RollsBackAndRethrows()
    {
        var quiz = BuildQuiz();
        SetupScenario(quiz, NewAttempt());
        _uow.Setup(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("db down"));

        var answers = BuildAnswers(quiz, correctCount: 2);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => Act(answers));

        Assert.Equal("db down", ex.Message);
        _uow.Verify(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// UTCID24 · B15/B16 nhiều vòng · Type B — câu hỏi nhiều đáp án đúng.
    /// Mỗi lựa chọn là một QuizAnswer riêng ⇒ AddAnswerAsync được gọi 2 lần.
    /// </summary>
    [Fact]
    public async Task UTCID24_MultipleSelectedOptions_PersistsOneAnswerPerOption()
    {
        var quiz = new QuizMaterial(MaterialId, 30, 60m, true);
        var question = new Question(MaterialId, "Chọn tất cả đáp án đúng", QuestionType.MultipleChoice, 1);
        question.AddAnswerOption(new AnswerOption(question.Id, "A", true, 1));
        question.AddAnswerOption(new AnswerOption(question.Id, "B", true, 2));
        question.AddAnswerOption(new AnswerOption(question.Id, "C", false, 3));
        quiz.AddQuestion(question);

        SetupScenario(quiz, NewAttempt());

        var correctIds = question.AnswerOptions.Where(o => o.IsCorrect).Select(o => o.Id).ToList();

        var result = await Act(new List<QuizAnswerSubmissionDto>
        {
            new() { QuestionId = question.Id, SelectedOptionIds = correctIds }
        });

        Assert.True(result.Success);
        Assert.Equal(100.00m, result.Data!.Score);
        _quizzes.Verify(x => x.AddAnswerAsync(It.IsAny<QuizAnswer>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}
