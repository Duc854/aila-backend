using AILA.Application.Common.Interfaces;
using AILA.Application.Common.Interfaces.Repositories;
using AILA.Application.Features.Quizzes;
using AILA.Application.Features.Quizzes.Commands.StartQuizAttempt;
using AILA.Application.Features.Quizzes.Commands.SubmitQuiz;
using AILA.Application.Features.Quizzes.Dtos;
using AILA.Application.Features.Quizzes.Queries.GetQuizResultDetail;
using AILA.Application.Features.Quizzes.Queries.GetQuizResultSummary;
using AILA.Application.Tests.Common.Builders;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace AILA.Application.Tests.Features.Quizzes
{
    /// <summary>
    /// Sheet: QuizAttemptService · UC-25 / UC-26 · TC-UNIT-QuizAttemptService-001 → 011.
    /// </summary>
    public class QuizAttemptHandlerTests
    {
        private static readonly Guid CourseId = Guid.NewGuid();
        private static readonly Guid MaterialId = Guid.NewGuid();
        private static readonly Guid LearnerId = Guid.NewGuid();

        private readonly Mock<IEnrollmentRepository> _enrollments = new();
        private readonly Mock<IQuizRepository> _quizzes = new();
        private readonly Mock<ILearningProgressRepository> _progresses = new();
        private readonly Mock<IUnitOfWork> _uow = new();

        public QuizAttemptHandlerTests()
        {
            _uow.Setup(u => u.Enrollments).Returns(_enrollments.Object);
            _uow.Setup(u => u.Quizzes).Returns(_quizzes.Object);
            _uow.Setup(u => u.LearningProgresses).Returns(_progresses.Object);
        }

        private StartQuizAttemptCommandHandler StartHandler()
            => new(_uow.Object, NullLogger<StartQuizAttemptCommandHandler>.Instance);

        private SubmitQuizCommandHandler SubmitHandler()
            => new(_uow.Object, NullLogger<SubmitQuizCommandHandler>.Instance);

        private void VerifyNotSaved()
            => _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

        private Enrollment ArrangeEnrollment(int totalMaterials = 4)
        {
            var enrollment = new Enrollment(LearnerId, CourseId, totalMaterials);
            _enrollments.Setup(r => r.GetByCourseAndLearnerAsync(CourseId, LearnerId, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(enrollment);
            return enrollment;
        }

        private QuizMaterial ArrangeQuiz(QuizMaterial quiz)
        {
            _quizzes.Setup(r => r.GetQuizForLearningAsync(CourseId, MaterialId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(quiz);
            return quiz;
        }

        /// <summary>Bài nộp chọn đúng đáp án đầu tiên cho <paramref name="correctCount"/> câu đầu.</summary>
        private static List<QuizAnswerSubmissionDto> AnswerFirstNCorrectly(QuizMaterial quiz, int correctCount)
        {
            var submissions = new List<QuizAnswerSubmissionDto>();
            var index = 0;

            foreach (var question in quiz.Questions)
            {
                var correctIds = question.AnswerOptions.Where(o => o.IsCorrect).Select(o => o.Id).ToList();
                var wrongId = question.AnswerOptions.First(o => !o.IsCorrect).Id;

                submissions.Add(new QuizAnswerSubmissionDto
                {
                    QuestionId = question.Id,
                    SelectedOptionIds = index < correctCount ? correctIds : new List<Guid> { wrongId }
                });
                index++;
            }

            return submissions;
        }

        // ============================================================ TC-001
        // Covers: Main Flow — tạo lượt làm bài In_Progress kèm hạn nộp do server tính.
        [Fact]
        [Trait("TC", "TC-UNIT-QuizAttemptService-001")]
        [Trait("UC", "UC-25")]
        [Trait("BR", "BR-02")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task Start_FirstTime_InProgressWithDeadline()
        {
            var enrollment = ArrangeEnrollment();
            var quiz = ArrangeQuiz(new QuizBuilder().ForMaterial(MaterialId)
                .WithTimeLimit(30).WithQuestion(4, 0).WithQuestion(4, 1).Build());
            _quizzes.Setup(r => r.GetInProgressAttemptAsync(enrollment.Id, MaterialId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync((QuizAttempt?)null);

            QuizAttempt? created = null;
            _quizzes.Setup(r => r.AddAttemptAsync(It.IsAny<QuizAttempt>(), It.IsAny<CancellationToken>()))
                    .Callback<QuizAttempt, CancellationToken>((a, _) => created = a)
                    .Returns(Task.CompletedTask);

            var result = await StartHandler().Handle(
                new StartQuizAttemptCommand(CourseId, MaterialId, LearnerId), CancellationToken.None);

            Assert.True(result.Success);
            Assert.NotNull(created);
            Assert.Equal(QuizAttemptStatus.InProgress, created!.Status);
            Assert.Equal(enrollment.Id, created.EnrollmentId);
            Assert.Equal(created.Id, result.Data!.AttemptId);
            // Hạn nộp = StartedAt + TimeLimit, do SERVER quyết định chứ không tin client.
            Assert.Equal(created.StartedAt.AddMinutes(quiz.TimeLimitMinutes), result.Data.DeadlineAt);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        // Ba nhánh chặn trước khi tạo lượt làm bài.
        [Fact]
        [Trait("TC", "TC-UNIT-QuizAttemptService-001")]
        [Trait("UC", "UC-25")]
        [Trait("Type", "Security (GBR)")]
        [Trait("Priority", "High")]
        public async Task Start_NotEnrolled_ReturnsEnrollmentNotFound()
        {
            _enrollments.Setup(r => r.GetByCourseAndLearnerAsync(
                            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync((Enrollment?)null);

            var result = await StartHandler().Handle(
                new StartQuizAttemptCommand(CourseId, MaterialId, LearnerId), CancellationToken.None);

            Assert.Equal("ENROLLMENT_NOT_FOUND", result.ErrorCode);
            _quizzes.Verify(r => r.GetQuizForLearningAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
            VerifyNotSaved();
        }

        // Quiz chưa cấu hình đủ (0 câu hỏi, hoặc có câu hỏi không đáp án) thì không cho bắt đầu.
        // Đây chính là hệ quả trực tiếp của DEF-QZ-01 (CreateQuestion không ép tối thiểu 2 đáp án).
        [Fact]
        [Trait("TC", "TC-UNIT-QuizAttemptService-001")]
        [Trait("UC", "UC-25")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task Start_QuestionNoOptions_NotConfigured()
        {
            ArrangeEnrollment();
            ArrangeQuiz(new QuizBuilder().ForMaterial(MaterialId)
                .WithQuestion(4, 0).WithQuestionWithoutOptions().Build());

            var result = await StartHandler().Handle(
                new StartQuizAttemptCommand(CourseId, MaterialId, LearnerId), CancellationToken.None);

            Assert.Equal("QUIZ_NOT_CONFIGURED", result.ErrorCode);
            _quizzes.Verify(r => r.AddAttemptAsync(It.IsAny<QuizAttempt>(), It.IsAny<CancellationToken>()), Times.Never);
            VerifyNotSaved();
        }

        // ============================================================ TC-002
        // Covers: resume — còn lượt In_Progress chưa hết giờ thì trả lại chính nó, không tạo mới.
        [Fact]
        [Trait("TC", "TC-UNIT-QuizAttemptService-002")]
        [Trait("UC", "UC-25")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        public async Task Start_LiveAttempt_ResumesNoNewAttempt()
        {
            var enrollment = ArrangeEnrollment();
            ArrangeQuiz(new QuizBuilder().ForMaterial(MaterialId).WithTimeLimit(30).WithQuestion(4, 0).Build());

            var existing = new QuizAttempt(enrollment.Id, MaterialId);   // StartedAt = bây giờ
            _quizzes.Setup(r => r.GetInProgressAttemptAsync(enrollment.Id, MaterialId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(existing);

            var result = await StartHandler().Handle(
                new StartQuizAttemptCommand(CourseId, MaterialId, LearnerId), CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal(existing.Id, result.Data!.AttemptId);
            _quizzes.Verify(r => r.AddAttemptAsync(It.IsAny<QuizAttempt>(), It.IsAny<CancellationToken>()), Times.Never);
            VerifyNotSaved();
        }

        // ------------------------------------------------------------ TC-012 (MỚI)
        // Nhánh "lượt cũ đã hết giờ mà chưa nộp" — handler mở lượt MỚI thay vì resume.
        // Không có TC nào trong workbook phủ.
        // → cần thêm dòng TC-UNIT-QuizAttemptService-012 vào sheet.
        [Fact]
        [Trait("TC", "TC-UNIT-QuizAttemptService-012")]
        [Trait("UC", "UC-25")]
        [Trait("Type", "Boundary & Negative")]
        [Trait("Priority", "Medium")]
        public async Task Start_ExpiredAttempt_OpensNewAttempt()
        {
            var enrollment = ArrangeEnrollment();
            ArrangeQuiz(new QuizBuilder().ForMaterial(MaterialId).WithTimeLimit(30).WithQuestion(4, 0).Build());

            var stale = new QuizAttempt(enrollment.Id, MaterialId);
            Common.TestEntity.SetProperty(stale, nameof(QuizAttempt.StartedAt), DateTime.UtcNow.AddMinutes(-31));
            _quizzes.Setup(r => r.GetInProgressAttemptAsync(enrollment.Id, MaterialId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(stale);

            QuizAttempt? created = null;
            _quizzes.Setup(r => r.AddAttemptAsync(It.IsAny<QuizAttempt>(), It.IsAny<CancellationToken>()))
                    .Callback<QuizAttempt, CancellationToken>((a, _) => created = a)
                    .Returns(Task.CompletedTask);

            var result = await StartHandler().Handle(
                new StartQuizAttemptCommand(CourseId, MaterialId, LearnerId), CancellationToken.None);

            Assert.True(result.Success);
            Assert.NotNull(created);
            Assert.NotEqual(stale.Id, created!.Id);
            Assert.Equal(created.Id, result.Data!.AttemptId);
        }

        // ============================================================ TC-008
        // Covers: BR-01 làm lại nhiều lần — lượt trước đã Submitted nên repo trả null cho
        // "in-progress", handler mở lượt mới. Không có giới hạn số lần.
        [Fact]
        [Trait("TC", "TC-UNIT-QuizAttemptService-008")]
        [Trait("UC", "UC-25")]
        [Trait("BR", "BR-01")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Low")]
        public async Task Start_AfterSubmitted_CreatesAnother()
        {
            var enrollment = ArrangeEnrollment();
            ArrangeQuiz(new QuizBuilder().ForMaterial(MaterialId).WithQuestion(4, 0).Build());
            _quizzes.Setup(r => r.GetInProgressAttemptAsync(enrollment.Id, MaterialId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync((QuizAttempt?)null);

            var result = await StartHandler().Handle(
                new StartQuizAttemptCommand(CourseId, MaterialId, LearnerId), CancellationToken.None);

            Assert.True(result.Success);
            _quizzes.Verify(r => r.AddAttemptAsync(It.IsAny<QuizAttempt>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        // ============================================================ TC-004 / TC-006
        // Covers: Main Flow / BR-04 / BR-05 — chấm điểm PHÍA SERVER, đạt thì cộng tiến độ.
        // Điểm = round(đúng / tổng * 100, 2). Trả lời đúng 2/2 → 100 ≥ 70 → đạt.
        [Fact]
        [Trait("TC", "TC-UNIT-QuizAttemptService-004")]
        [Trait("UC", "UC-25")]
        [Trait("BR", "BR-04")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Critical")]
        public async Task Submit_AllCorrect_GradesAndCommits()
        {
            var enrollment = ArrangeEnrollment(totalMaterials: 4);
            var quiz = ArrangeQuiz(new QuizBuilder().ForMaterial(MaterialId)
                .WithPassingScore(70).WithQuestion(4, 0).WithQuestion(4, 1).Build());

            var attempt = new QuizAttempt(enrollment.Id, MaterialId);
            _quizzes.Setup(r => r.GetAttemptWithAnswersAsync(attempt.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(attempt);
            _progresses.Setup(r => r.GetByCompositeKeyAsync(enrollment.Id, MaterialId, It.IsAny<CancellationToken>()))
                       .ReturnsAsync((LearningProgress?)null);

            var result = await SubmitHandler().Handle(
                new SubmitQuizCommand(CourseId, MaterialId, attempt.Id, LearnerId,
                                      AnswerFirstNCorrectly(quiz, 2)),
                CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal(100m, result.Data!.Score);
            Assert.True(result.Data.IsPassed);
            Assert.Equal(2, result.Data.CorrectAnswers);
            Assert.Equal(2, result.Data.TotalQuestions);
            Assert.Equal(QuizAttemptStatus.Submitted, attempt.Status);
            Assert.Equal(1, enrollment.CompletedMaterials);   // đạt → cộng tiến độ
            _uow.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
            _uow.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        // ============================================================ TC-007
        // Covers: BR-05 nhánh FALSE.
        // ⚠ Notes workbook ("progress VẪN Complete cho MỌI lần nộp, không phụ thuộc pass/fail")
        // đã LỖI THỜI: handler hiện có `if (isPassed)` bao quanh phần cập nhật tiến độ.
        // Trượt ⇒ attempt vẫn Submitted nhưng KHÔNG hoàn thành học liệu — đúng như UCS.
        [Fact]
        [Trait("TC", "TC-UNIT-QuizAttemptService-007")]
        [Trait("UC", "UC-25")]
        [Trait("BR", "BR-05")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task Submit_BelowPassing_NoProgressAdvance()
        {
            var enrollment = ArrangeEnrollment(totalMaterials: 4);
            var quiz = ArrangeQuiz(new QuizBuilder().ForMaterial(MaterialId)
                .WithPassingScore(70)
                .WithQuestion(4, 0).WithQuestion(4, 1).WithQuestion(4, 2).WithQuestion(4, 3)
                .Build());

            var attempt = new QuizAttempt(enrollment.Id, MaterialId);
            _quizzes.Setup(r => r.GetAttemptWithAnswersAsync(attempt.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(attempt);

            // Đúng 1/4 → 25 < 70 → trượt.
            var result = await SubmitHandler().Handle(
                new SubmitQuizCommand(CourseId, MaterialId, attempt.Id, LearnerId,
                                      AnswerFirstNCorrectly(quiz, 1)),
                CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal(25m, result.Data!.Score);
            Assert.False(result.Data.IsPassed);
            Assert.Equal(QuizAttemptStatus.Submitted, attempt.Status);
            Assert.Equal(0, enrollment.CompletedMaterials);   // trượt → KHÔNG cộng tiến độ
            _progresses.Verify(r => r.GetByCompositeKeyAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
            _uow.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        // Biên PassingScore: đúng ngưỡng là ĐẠT (>=, không phải >).
        [Fact]
        [Trait("TC", "TC-UNIT-QuizAttemptService-006")]
        [Trait("UC", "UC-25")]
        [Trait("BR", "BR-05")]
        [Trait("Type", "Boundary & Negative")]
        [Trait("Priority", "High")]
        public async Task Submit_ScoreAtPassing_CountsAsPassed()
        {
            var enrollment = ArrangeEnrollment();
            var quiz = ArrangeQuiz(new QuizBuilder().ForMaterial(MaterialId)
                .WithPassingScore(50)
                .WithQuestion(4, 0).WithQuestion(4, 1)
                .Build());

            var attempt = new QuizAttempt(enrollment.Id, MaterialId);
            _quizzes.Setup(r => r.GetAttemptWithAnswersAsync(attempt.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(attempt);
            _progresses.Setup(r => r.GetByCompositeKeyAsync(
                        It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync((LearningProgress?)null);

            var result = await SubmitHandler().Handle(
                new SubmitQuizCommand(CourseId, MaterialId, attempt.Id, LearnerId,
                                      AnswerFirstNCorrectly(quiz, 1)),   // 1/2 → 50
                CancellationToken.None);

            Assert.Equal(50m, result.Data!.Score);
            Assert.True(result.Data.IsPassed);
        }

        // ============================================================ TC-005
        // Covers: AF-01 hết giờ.
        // ⚠ KHÔNG có trigger auto-submit phía server: nếu client không gửi request thì bài
        // không bao giờ được nộp. Server chỉ tính cờ WasAutoSubmitted để ghi log, còn lại
        // xử lý y hệt submit thường — vẫn chấm, vẫn cho đạt.
        [Fact(Skip = "DEF-QA-01 - No server-side auto-submit when the time limit expires")]
        [Trait("TC", "TC-UNIT-QuizAttemptService-005")]
        [Trait("UC", "UC-25")]
        [Trait("Type", "Boundary & Negative")]
        [Trait("Priority", "High")]
        [Trait("Defect", "DEF-QA-01")]
        public async Task Submit_AfterDeadline_GradesFlagsAuto()
        {
            var enrollment = ArrangeEnrollment();
            var quiz = ArrangeQuiz(new QuizBuilder().ForMaterial(MaterialId)
                .WithTimeLimit(30).WithPassingScore(70).WithQuestion(4, 0).Build());

            var attempt = new QuizAttempt(enrollment.Id, MaterialId);
            Common.TestEntity.SetProperty(attempt, nameof(QuizAttempt.StartedAt), DateTime.UtcNow.AddMinutes(-31));
            _quizzes.Setup(r => r.GetAttemptWithAnswersAsync(attempt.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(attempt);
            _progresses.Setup(r => r.GetByCompositeKeyAsync(
                        It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync((LearningProgress?)null);

            var result = await SubmitHandler().Handle(
                new SubmitQuizCommand(CourseId, MaterialId, attempt.Id, LearnerId,
                                      AnswerFirstNCorrectly(quiz, 1)),
                CancellationToken.None);

            Assert.True(result.Success);
            Assert.True(result.Data!.WasAutoSubmitted);
            Assert.Equal(100m, result.Data.Score);    // nộp muộn vẫn được chấm đầy đủ
            Assert.True(result.Data.IsPassed);
        }

        // ------------------------------------------------------------ Bảo mật: lượt của người khác
        [Fact]
        [Trait("TC", "TC-UNIT-QuizAttemptService-004")]
        [Trait("UC", "UC-25")]
        [Trait("Type", "Security (GBR)")]
        [Trait("Priority", "High")]
        public async Task Submit_ForeignAttempt_Forbidden()
        {
            ArrangeEnrollment();
            var foreignAttempt = new QuizAttempt(Guid.NewGuid(), MaterialId);
            _quizzes.Setup(r => r.GetAttemptWithAnswersAsync(foreignAttempt.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(foreignAttempt);

            var result = await SubmitHandler().Handle(
                new SubmitQuizCommand(CourseId, MaterialId, foreignAttempt.Id, LearnerId,
                                      new List<QuizAnswerSubmissionDto>()),
                CancellationToken.None);

            Assert.Equal("FORBIDDEN", result.ErrorCode);
            _uow.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        // Bài nộp chứa câu hỏi/đáp án không thuộc quiz → từ chối, không mở transaction.
        [Fact]
        [Trait("TC", "TC-UNIT-QuizAttemptService-004")]
        [Trait("UC", "UC-25")]
        [Trait("Type", "Input Validation")]
        [Trait("Priority", "High")]
        public async Task Submit_ForeignQuestion_InvalidSubmit()
        {
            var enrollment = ArrangeEnrollment();
            ArrangeQuiz(new QuizBuilder().ForMaterial(MaterialId).WithQuestion(4, 0).Build());

            var attempt = new QuizAttempt(enrollment.Id, MaterialId);
            _quizzes.Setup(r => r.GetAttemptWithAnswersAsync(attempt.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(attempt);

            var result = await SubmitHandler().Handle(
                new SubmitQuizCommand(CourseId, MaterialId, attempt.Id, LearnerId,
                    new List<QuizAnswerSubmissionDto>
                    {
                        new() { QuestionId = Guid.NewGuid(), SelectedOptionIds = new List<Guid>() }
                    }),
                CancellationToken.None);

            Assert.Equal("INVALID_SUBMISSION", result.ErrorCode);
            _uow.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        // Quiz thiếu đáp án đúng → không chấm được, từ chối thay vì cho 0 điểm.
        [Fact]
        [Trait("TC", "TC-UNIT-QuizAttemptService-004")]
        [Trait("UC", "UC-25")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task Submit_NoCorrectAnswer_NotConfigured()
        {
            var enrollment = ArrangeEnrollment();
            ArrangeQuiz(new QuizBuilder().ForMaterial(MaterialId)
                .WithQuestion(4, 0).WithQuestionWithoutCorrectAnswer().Build());

            var attempt = new QuizAttempt(enrollment.Id, MaterialId);
            _quizzes.Setup(r => r.GetAttemptWithAnswersAsync(attempt.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(attempt);

            var result = await SubmitHandler().Handle(
                new SubmitQuizCommand(CourseId, MaterialId, attempt.Id, LearnerId,
                                      new List<QuizAnswerSubmissionDto>()),
                CancellationToken.None);

            Assert.Equal("QUIZ_NOT_CONFIGURED", result.ErrorCode);
            _uow.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        // Double-submit: lượt đã nộp thì trả lại kết quả cũ, KHÔNG chấm đè.
        [Fact]
        [Trait("TC", "TC-UNIT-QuizAttemptService-004")]
        [Trait("UC", "UC-25")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task Submit_AlreadySubmitted_KeepsScore()
        {
            var enrollment = ArrangeEnrollment();
            var quiz = ArrangeQuiz(new QuizBuilder().ForMaterial(MaterialId)
                .WithPassingScore(70).WithQuestion(4, 0).Build());

            var attempt = new QuizAttempt(enrollment.Id, MaterialId);
            attempt.Submit(42m, false);                       // đã nộp trước đó với điểm 42
            _quizzes.Setup(r => r.GetAttemptWithAnswersAsync(attempt.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(attempt);

            var result = await SubmitHandler().Handle(
                new SubmitQuizCommand(CourseId, MaterialId, attempt.Id, LearnerId,
                                      AnswerFirstNCorrectly(quiz, 1)),   // gửi bài toàn đúng
                CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal(42m, result.Data!.Score);    // điểm cũ được giữ, không bị chấm đè
            Assert.False(result.Data.IsPassed);
            _uow.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
            VerifyNotSaved();
        }

        // ============================================================ TC-009
        // Covers: Main Flow / BR-01 — trả kết quả của lượt nộp GẦN NHẤT.
        // Số câu đúng được TÍNH LẠI từ đáp án đã lưu (QuizGrading), không đọc từ cột nào —
        // nên nếu expert sửa đáp án đúng sau đó, con số này đổi theo. Đó là hệ quả thật của
        // việc không có versioning đáp án (cùng họ DEF-MAT-01).
        [Fact]
        [Trait("TC", "TC-UNIT-QuizAttemptService-009")]
        [Trait("UC", "UC-26")]
        [Trait("BR", "BR-01")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        public async Task ResultSummary_Submitted_ScoreAndCorrect()
        {
            var enrollment = ArrangeEnrollment();
            var quiz = ArrangeQuiz(new QuizBuilder().ForMaterial(MaterialId)
                .WithPassingScore(70).WithQuestion(4, 0).WithQuestion(4, 1).Build());

            // Lượt đã nộp: trả lời đúng câu 1, sai câu 2.
            var attempt = new QuizAttempt(enrollment.Id, MaterialId);
            var q1 = quiz.Questions.ElementAt(0);
            var q2 = quiz.Questions.ElementAt(1);
            attempt.AddAnswer(new QuizAnswer(attempt.Id, q1.Id,
                q1.AnswerOptions.First(o => o.IsCorrect).Id));
            attempt.AddAnswer(new QuizAnswer(attempt.Id, q2.Id,
                q2.AnswerOptions.First(o => !o.IsCorrect).Id));
            attempt.Submit(50m, false);

            _quizzes.Setup(r => r.GetLatestSubmittedAttemptAsync(
                        enrollment.Id, MaterialId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(attempt);

            var result = await new GetQuizResultSummaryQueryHandler(_uow.Object).Handle(
                new GetQuizResultSummaryQuery(CourseId, MaterialId, LearnerId), CancellationToken.None);

            Assert.True(result.Success);
            Assert.True(result.Data!.HasResult);
            Assert.Equal(attempt.Id, result.Data.AttemptId);
            Assert.Equal(50m, result.Data.Score);
            Assert.False(result.Data.IsPassed);
            Assert.Equal(70m, result.Data.PassingScore);
            Assert.Equal(2, result.Data.TotalQuestions);
            Assert.Equal(1, result.Data.CorrectAnswers);          // tính lại từ đáp án đã lưu
            Assert.True(result.Data.CanViewDetails);              // quiz cho xem lại đáp án
            Assert.NotNull(result.Data.SubmittedAt);
        }

        // ============================================================ TC-011
        // Covers: AF-01 — chưa làm bài KHÔNG phải lỗi. Trả HasResult=false kèm metadata
        // (tổng số câu, điểm đạt) để UI hiện đúng trạng thái "chưa làm" mà không cần gọi thêm.
        [Fact]
        [Trait("TC", "TC-UNIT-QuizAttemptService-011")]
        [Trait("UC", "UC-26")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        public async Task ResultSummary_NoAttempt_EmptyState()
        {
            var enrollment = ArrangeEnrollment();
            ArrangeQuiz(new QuizBuilder().ForMaterial(MaterialId)
                .WithPassingScore(70).WithQuestion(4, 0).WithQuestion(4, 1).WithQuestion(4, 2).Build());
            _quizzes.Setup(r => r.GetLatestSubmittedAttemptAsync(
                        enrollment.Id, MaterialId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync((QuizAttempt?)null);

            var result = await new GetQuizResultSummaryQueryHandler(_uow.Object).Handle(
                new GetQuizResultSummaryQuery(CourseId, MaterialId, LearnerId), CancellationToken.None);

            Assert.True(result.Success);
            Assert.Null(result.ErrorCode);
            Assert.False(result.Data!.HasResult);
            Assert.Null(result.Data.AttemptId);
            Assert.Null(result.Data.Score);
            Assert.Equal(3, result.Data.TotalQuestions);          // metadata vẫn có
            Assert.Equal(70m, result.Data.PassingScore);
        }

        [Fact]
        [Trait("TC", "TC-UNIT-QuizAttemptService-011")]
        [Trait("UC", "UC-26")]
        [Trait("Type", "Security (GBR)")]
        [Trait("Priority", "High")]
        public async Task ResultSummary_NotEnrolled_Rejected()
        {
            _enrollments.Setup(r => r.GetByCourseAndLearnerAsync(
                        It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync((Enrollment?)null);

            var result = await new GetQuizResultSummaryQueryHandler(_uow.Object).Handle(
                new GetQuizResultSummaryQuery(CourseId, MaterialId, LearnerId), CancellationToken.None);

            Assert.Equal("ENROLLMENT_NOT_FOUND", result.ErrorCode);
            _quizzes.Verify(r => r.GetQuizForLearningAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        // ============================================================ TC-010
        // Covers: BR-02 — xem chi tiết thì thấy cả lựa chọn của mình và đáp án đúng.
        [Fact]
        [Trait("TC", "TC-UNIT-QuizAttemptService-010")]
        [Trait("UC", "UC-26")]
        [Trait("BR", "BR-02")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        public async Task ResultDetail_AnswersVisible_ShowsChoice()
        {
            var enrollment = ArrangeEnrollment();
            var quiz = ArrangeQuiz(new QuizBuilder().ForMaterial(MaterialId)
                .WithPassingScore(70).WithQuestion(4, 0).WithQuestion(4, 1).Build());

            var attempt = new QuizAttempt(enrollment.Id, MaterialId);
            var q1 = quiz.Questions.ElementAt(0);
            var chosenCorrect = q1.AnswerOptions.First(o => o.IsCorrect).Id;
            attempt.AddAnswer(new QuizAnswer(attempt.Id, q1.Id, chosenCorrect));
            attempt.Submit(50m, false);

            _quizzes.Setup(r => r.GetLatestSubmittedAttemptAsync(
                        enrollment.Id, MaterialId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(attempt);

            var result = await new GetQuizResultDetailQueryHandler(
                    _uow.Object, NullLogger<GetQuizResultDetailQueryHandler>.Instance)
                .Handle(new GetQuizResultDetailQuery(CourseId, MaterialId, LearnerId),
                        CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal(attempt.Id, result.Data!.AttemptId);
            Assert.Equal(2, result.Data.Questions.Count);

            var answered = result.Data.Questions.Single(q => q.QuestionId == q1.Id);
            Assert.Contains(chosenCorrect, answered.SelectedOptionIds);
            Assert.True(answered.IsCorrect);
            Assert.Equal(4, answered.Options.Count);              // đủ 4 đáp án để đối chiếu

            // Câu bỏ trống vẫn xuất hiện, đánh dấu sai.
            var skipped = result.Data.Questions.Single(q => q.QuestionId != q1.Id);
            Assert.Empty(skipped.SelectedOptionIds);
            Assert.False(skipped.IsCorrect);
        }

        // Quiz cấu hình ẩn đáp án ⇒ chặn ngay, KHÔNG rò rỉ đáp án đúng qua endpoint này.
        [Fact]
        [Trait("TC", "TC-UNIT-QuizAttemptService-010")]
        [Trait("UC", "UC-26")]
        [Trait("BR", "BR-02")]
        [Trait("Type", "Security (GBR)")]
        [Trait("Priority", "High")]
        public async Task ResultDetail_AnswersHidden_Rejected()
        {
            var enrollment = ArrangeEnrollment();
            ArrangeQuiz(new QuizBuilder().ForMaterial(MaterialId)
                .HideAnswers().WithQuestion(4, 0).Build());

            var result = await new GetQuizResultDetailQueryHandler(
                    _uow.Object, NullLogger<GetQuizResultDetailQueryHandler>.Instance)
                .Handle(new GetQuizResultDetailQuery(CourseId, MaterialId, LearnerId),
                        CancellationToken.None);

            Assert.False(result.Success);
            Assert.Equal("ANSWERS_HIDDEN", result.ErrorCode);
            Assert.Null(result.Data);
            // Chặn trước khi cả nạp lượt làm bài — không tốn truy vấn, không lộ gì.
            _quizzes.Verify(r => r.GetLatestSubmittedAttemptAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        [Trait("TC", "TC-UNIT-QuizAttemptService-010")]
        [Trait("UC", "UC-26")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        public async Task ResultDetail_NoAttempt_NoResult()
        {
            var enrollment = ArrangeEnrollment();
            ArrangeQuiz(new QuizBuilder().ForMaterial(MaterialId).WithQuestion(4, 0).Build());
            _quizzes.Setup(r => r.GetLatestSubmittedAttemptAsync(
                        enrollment.Id, MaterialId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync((QuizAttempt?)null);

            var result = await new GetQuizResultDetailQueryHandler(
                    _uow.Object, NullLogger<GetQuizResultDetailQueryHandler>.Instance)
                .Handle(new GetQuizResultDetailQuery(CourseId, MaterialId, LearnerId),
                        CancellationToken.None);

            Assert.Equal("NO_RESULT", result.ErrorCode);
        }
    }

    /// <summary>
    /// Logic chấm điểm là static thuần — test trực tiếp, không cần mock gì.
    /// Đây là phần rẻ nhất và có giá trị cao nhất của cả sheet.
    /// </summary>
    public class QuizGradingTests
    {
        private static Question QuestionWith(int optionCount, params int[] correctIndexes)
        {
            var question = new Question(Guid.NewGuid(), "Câu hỏi", QuestionType.MultipleChoice, 1);
            for (var i = 0; i < optionCount; i++)
                question.AddAnswerOption(
                    new AnswerOption(question.Id, $"Đáp án {i + 1}", correctIndexes.Contains(i), i + 1));
            return question;
        }

        [Fact]
        [Trait("TC", "TC-UNIT-QuizAttemptService-004")]
        [Trait("Type", "Functional")]
        public void IsAnswerCorrect_ExactMatch_True()
        {
            var question = QuestionWith(4, 0, 2);
            var correctIds = question.AnswerOptions.Where(o => o.IsCorrect).Select(o => o.Id);

            Assert.True(QuizGrading.IsAnswerCorrect(question, correctIds));
        }

        // Multiple-choice tính đúng theo TẬP HỢP: thiếu một đáp án đúng cũng là sai,
        // chọn thừa một đáp án sai cũng là sai. Không có điểm từng phần.
        [Fact]
        [Trait("Type", "Functional")]
        public void IsAnswerCorrect_Partial_False()
        {
            var question = QuestionWith(4, 0, 2);
            var onlyOneCorrect = question.AnswerOptions.Where(o => o.IsCorrect).Take(1).Select(o => o.Id);

            Assert.False(QuizGrading.IsAnswerCorrect(question, onlyOneCorrect));
        }

        [Fact]
        [Trait("Type", "Functional")]
        public void IsAnswerCorrect_ExtraWrong_False()
        {
            var question = QuestionWith(4, 0);
            var ids = question.AnswerOptions.Take(2).Select(o => o.Id);   // 1 đúng + 1 sai

            Assert.False(QuizGrading.IsAnswerCorrect(question, ids));
        }

        [Fact]
        [Trait("Type", "Boundary & Negative")]
        public void IsAnswerCorrect_NoSelection_ReturnsFalse()
        {
            var question = QuestionWith(4, 0);

            Assert.False(QuizGrading.IsAnswerCorrect(question, Array.Empty<Guid>()));
            Assert.False(QuizGrading.IsAnswerCorrect(question, null!));
        }

        // Câu hỏi không có đáp án đúng nào thì KHÔNG thể chấm — luôn tính là sai,
        // kể cả khi người học chọn hết. Ngăn việc "đoán bừa vẫn đúng".
        [Fact]
        [Trait("Type", "Boundary & Negative")]
        public void IsAnswerCorrect_NoCorrectOption_False()
        {
            var question = QuestionWith(4);
            var allIds = question.AnswerOptions.Select(o => o.Id);

            Assert.False(QuizGrading.IsAnswerCorrect(question, allIds));
        }

        // Câu không trả lời vẫn được tính vào mẫu số → bỏ trống là mất điểm.
        [Fact]
        [Trait("Type", "Functional")]
        public void CountCorrect_UnansweredQuestionsCountAsWrong()
        {
            var q1 = QuestionWith(4, 0);
            var q2 = QuestionWith(4, 1);
            var byId = new Dictionary<Guid, Question> { [q1.Id] = q1, [q2.Id] = q2 };
            var selections = new Dictionary<Guid, List<Guid>>
            {
                [q1.Id] = q1.AnswerOptions.Where(o => o.IsCorrect).Select(o => o.Id).ToList()
            };

            Assert.Equal(1, QuizGrading.CountCorrect(byId, selections));
        }
    }
}
