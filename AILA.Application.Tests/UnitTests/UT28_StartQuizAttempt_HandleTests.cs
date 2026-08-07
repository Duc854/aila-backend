using AILA.Application.Common.Interfaces;
using AILA.Application.Common.Interfaces.Repositories;
using AILA.Application.Features.Quizzes.Commands.StartQuizAttempt;
using AILA.Application.Tests.UnitTests.TestHelpers;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;

namespace AILA.Application.Tests.UnitTests;

/// <summary>
/// Sheet: UT28_StartQuizAttempt — <see cref="StartQuizAttemptCommandHandler.Handle"/>
/// Module: Quiz · CC = 7 · 8 test case
///
/// Nhánh: B1 = enrollment null · B2 = quiz null
///        B3 = quiz không có câu hỏi · B4 = có câu hỏi thiếu đáp án
///        B5 = chưa có attempt · B6 = attempt cũ đã hết giờ
///
/// Bất biến AF-01: chỉ TIẾP TỤC lượt đang dở khi nó CHƯA hết giờ. Lượt đã quá hạn phải mở
/// lượt MỚI — nếu không, client nhận DeadlineAt trong quá khứ và không thể làm bài.
/// Toán tử so sánh là &lt;= nên thời điểm ĐÚNG deadline đã tính là hết giờ (xem UTCID08).
/// </summary>
public class UT28_StartQuizAttempt_HandleTests
{
    private static readonly Guid CourseId = Guid.NewGuid();
    private static readonly Guid MaterialId = Guid.NewGuid();
    private static readonly Guid LearnerId = Guid.NewGuid();
    private const int TimeLimitMinutes = 30;

    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IEnrollmentRepository> _enrollments = new();
    private readonly Mock<IQuizRepository> _quizzes = new();

    private readonly Enrollment _enrollment = new(LearnerId, CourseId, totalMaterials: 3);

    public UT28_StartQuizAttempt_HandleTests()
    {
        _uow.SetupGet(x => x.Enrollments).Returns(_enrollments.Object);
        _uow.SetupGet(x => x.Quizzes).Returns(_quizzes.Object);
        _uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _enrollments.Setup(x => x.GetByCourseAndLearnerAsync(CourseId, LearnerId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(_enrollment);
        _quizzes.Setup(x => x.GetInProgressAttemptAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((QuizAttempt?)null);
    }

    private StartQuizAttemptCommandHandler CreateSut() =>
        new(_uow.Object, Mock.Of<ILogger<StartQuizAttemptCommandHandler>>());

    /// <summary>Quiz với n câu hỏi; withOptions = false ⇒ câu hỏi không có đáp án nào.</summary>
    private static QuizMaterial BuildQuiz(int questionCount = 2, bool withOptions = true)
    {
        var quiz = new QuizMaterial(MaterialId, TimeLimitMinutes, 60m, true);
        for (var i = 0; i < questionCount; i++)
        {
            var q = new Question(MaterialId, $"Câu hỏi {i + 1}", QuestionType.SingleChoice, questionCount - i);
            if (withOptions)
            {
                q.AddAnswerOption(new AnswerOption(q.Id, "Đáp án A", true, 1));
                q.AddAnswerOption(new AnswerOption(q.Id, "Đáp án B", false, 2));
            }
            quiz.AddQuestion(q);
        }
        return quiz;
    }

    private void SetupQuiz(QuizMaterial? quiz) =>
        _quizzes.Setup(x => x.GetQuizForLearningAsync(CourseId, MaterialId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(quiz);

    private QuizAttempt SetupExistingAttempt(int startedMinutesAgo)
    {
        var attempt = new QuizAttempt(_enrollment.Id, MaterialId);
        PrivateSetter.Set(attempt, nameof(QuizAttempt.StartedAt), DateTime.UtcNow.AddMinutes(-startedMinutesAgo));
        _quizzes.Setup(x => x.GetInProgressAttemptAsync(_enrollment.Id, MaterialId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(attempt);
        return attempt;
    }

    private Task<Shared.Wrappers.ResponseDto<Features.Quizzes.Dtos.StartQuizResponseDto>> Act() =>
        CreateSut().Handle(new StartQuizAttemptCommand(CourseId, MaterialId, LearnerId), CancellationToken.None);

    /// <summary>UTCID01 · B1=T · Type A — chưa ghi danh khóa học.</summary>
    [Fact]
    public async Task UTCID01_EnrollmentNotFound_ReturnsEnrollmentNotFound()
    {
        _enrollments.Setup(x => x.GetByCourseAndLearnerAsync(CourseId, LearnerId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync((Enrollment?)null);

        var result = await Act();

        Assert.False(result.Success);
        Assert.Equal("ENROLLMENT_NOT_FOUND", result.ErrorCode);
    }

    /// <summary>UTCID02 · B2=T · Type A — bài kiểm tra không thuộc khóa học.</summary>
    [Fact]
    public async Task UTCID02_QuizNotFound_ReturnsQuizNotFound()
    {
        SetupQuiz(null);

        var result = await Act();

        Assert.False(result.Success);
        Assert.Equal("QUIZ_NOT_FOUND", result.ErrorCode);
    }

    /// <summary>UTCID03 · B3=T · Type B — bài kiểm tra rỗng (0 câu hỏi).</summary>
    [Fact]
    public async Task UTCID03_QuizWithoutQuestions_ReturnsQuizNotConfigured()
    {
        SetupQuiz(BuildQuiz(questionCount: 0));

        var result = await Act();

        Assert.False(result.Success);
        Assert.Equal("QUIZ_NOT_CONFIGURED", result.ErrorCode);
        _quizzes.Verify(x => x.AddAttemptAsync(It.IsAny<QuizAttempt>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>UTCID04 · B3=F, B4=T · Type A — có câu hỏi nhưng chưa nhập đáp án.</summary>
    [Fact]
    public async Task UTCID04_QuestionWithoutOptions_ReturnsQuizNotConfigured()
    {
        SetupQuiz(BuildQuiz(questionCount: 1, withOptions: false));

        var result = await Act();

        Assert.False(result.Success);
        Assert.Equal("QUIZ_NOT_CONFIGURED", result.ErrorCode);
    }

    /// <summary>
    /// UTCID05 · B5=T · Type N — chưa có lượt nào: tạo lượt mới.
    /// Kiểm tra thêm: câu hỏi trả về được sắp xếp theo OrderIndex tăng dần.
    /// </summary>
    [Fact]
    public async Task UTCID05_NoExistingAttempt_CreatesNewAttemptAndOrdersQuestions()
    {
        var quiz = BuildQuiz(questionCount: 2);
        SetupQuiz(quiz);

        var result = await Act();

        Assert.True(result.Success);
        Assert.Equal(MaterialId, result.Data!.MaterialId);
        Assert.Equal(TimeLimitMinutes, result.Data.TimeLimitMinutes);
        Assert.Equal(result.Data.StartedAt.AddMinutes(TimeLimitMinutes), result.Data.DeadlineAt);
        Assert.Equal(new[] { 1, 2 }, result.Data.Questions.Select(q => q.OrderIndex).ToArray());

        _quizzes.Verify(x => x.AddAttemptAsync(It.IsAny<QuizAttempt>(), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>UTCID06 · B5=F, B6=F · Type N — tiếp tục lượt đang dở còn thời gian.</summary>
    [Fact]
    public async Task UTCID06_InProgressAttemptStillValid_ResumesWithoutCreatingNew()
    {
        SetupQuiz(BuildQuiz());
        var existing = SetupExistingAttempt(startedMinutesAgo: 5);

        var result = await Act();

        Assert.True(result.Success);
        Assert.Equal(existing.Id, result.Data!.AttemptId);
        _quizzes.Verify(x => x.AddAttemptAsync(It.IsAny<QuizAttempt>(), It.IsAny<CancellationToken>()), Times.Never);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// UTCID07 · B6=T · Type B — lượt cũ đã quá hạn: phải mở lượt MỚI.
    /// Nếu tái sử dụng lượt cũ thì DeadlineAt sẽ nằm trong quá khứ (AF-01 bị vi phạm).
    /// </summary>
    [Fact]
    public async Task UTCID07_ExpiredAttempt_CreatesBrandNewAttempt()
    {
        SetupQuiz(BuildQuiz());
        var expired = SetupExistingAttempt(startedMinutesAgo: 60);

        var result = await Act();

        Assert.True(result.Success);
        Assert.NotEqual(expired.Id, result.Data!.AttemptId);
        Assert.True(result.Data.DeadlineAt > DateTime.UtcNow);
        _quizzes.Verify(x => x.AddAttemptAsync(It.IsAny<QuizAttempt>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// UTCID08 · B6=T · Type B — lượt cũ bắt đầu ĐÚNG TimeLimitMinutes trước.
    /// Toán tử là &lt;= nên thời điểm chạm deadline đã tính là hết giờ ⇒ vẫn mở lượt mới.
    /// </summary>
    [Fact]
    public async Task UTCID08_AttemptExactlyAtDeadline_IsTreatedAsExpired()
    {
        SetupQuiz(BuildQuiz());
        var expired = SetupExistingAttempt(startedMinutesAgo: TimeLimitMinutes);

        var result = await Act();

        Assert.True(result.Success);
        Assert.NotEqual(expired.Id, result.Data!.AttemptId);
        _quizzes.Verify(x => x.AddAttemptAsync(It.IsAny<QuizAttempt>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
