using AILA.Application.Common.Interfaces;
using AILA.Application.Features.Quizzes.Dtos;
using AILA.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Wrappers;

namespace AILA.Application.Features.Quizzes.Commands.SubmitQuiz
{
    public class SubmitQuizCommandHandler
        : IRequestHandler<SubmitQuizCommand, ResponseDto<QuizResultDto>>
    {
        private readonly IUnitOfWork _uow;
        private readonly ILogger<SubmitQuizCommandHandler> _logger;

        public SubmitQuizCommandHandler(
            IUnitOfWork uow,
            ILogger<SubmitQuizCommandHandler> logger)
        {
            _uow = uow;
            _logger = logger;
        }

        public async Task<ResponseDto<QuizResultDto>> Handle(
            SubmitQuizCommand request, CancellationToken cancellationToken)
        {
            // 1. Kiểm soát quyền: chỉ Learner đã enroll (BR-01, AC-7).
            var enrollment = await _uow.Enrollments.GetByCourseAndLearnerAsync(
                request.CourseId, request.LearnerId, cancellationToken);
            if (enrollment == null)
            {
                return ResponseDto<QuizResultDto>.FailResult(
                    "ENROLLMENT_NOT_FOUND", "Bạn chưa đăng ký tham gia khóa học này.");
            }

            // 2. Nạp lượt làm bài kèm câu trả lời (tracking để cập nhật).
            var attempt = await _uow.Quizzes.GetAttemptWithAnswersAsync(
                request.AttemptId, cancellationToken);
            if (attempt == null)
            {
                return ResponseDto<QuizResultDto>.FailResult(
                    "ATTEMPT_NOT_FOUND", "Không tìm thấy lượt làm bài kiểm tra.");
            }

            // 3. Lượt làm bài phải thuộc về chính chủ và đúng bài kiểm tra (NFR security).
            if (attempt.EnrollmentId != enrollment.Id || attempt.QuizMaterialId != request.MaterialId)
            {
                return ResponseDto<QuizResultDto>.FailResult(
                    "FORBIDDEN", "Bạn không có quyền truy cập lượt làm bài này.");
            }

            // 4. Nạp bài kiểm tra kèm câu hỏi + đáp án để chấm điểm phía server.
            var quiz = await _uow.Quizzes.GetQuizForLearningAsync(
                request.CourseId, request.MaterialId, cancellationToken);
            if (quiz == null)
            {
                return ResponseDto<QuizResultDto>.FailResult(
                    "QUIZ_NOT_FOUND", "Không tìm thấy bài kiểm tra trong khóa học này.");
            }

            // Thiếu cấu hình/đáp án (AF-02): không sinh kết quả, không cập nhật progress.
            if (!quiz.Questions.Any()
                || quiz.Questions.Any(q => q.AnswerOptions.All(o => !o.IsCorrect)))
            {
                _logger.LogWarning(
                    "Quiz {MaterialId} thiếu cấu hình đáp án đúng, từ chối chấm điểm.", request.MaterialId);
                return ResponseDto<QuizResultDto>.FailResult(
                    "QUIZ_NOT_CONFIGURED", "Bài kiểm tra chưa được cấu hình đầy đủ đáp án đúng.");
            }

            var questionsById = quiz.Questions.ToDictionary(q => q.Id);

            // 5. Idempotent / double-submit: nếu đã nộp thì trả lại kết quả cũ (không đè, giữ bất biến - AC-4).
            if (attempt.Status == Domain.Enums.QuizAttemptStatus.Submitted)
            {
                _logger.LogInformation(
                    "QuizAttempt {AttemptId} đã được nộp trước đó, trả về kết quả sẵn có (idempotent).",
                    attempt.Id);

                var previousSelections = attempt.Answers
                    .Where(a => a.SelectedAnswerOptionId.HasValue)
                    .GroupBy(a => a.QuestionId)
                    .ToDictionary(g => g.Key, g => g.First().SelectedAnswerOptionId);

                var previousCorrect = CountCorrect(questionsById, previousSelections);

                return ResponseDto<QuizResultDto>.SuccessResult(
                    BuildResult(attempt, quiz, enrollment, previousCorrect, wasAutoSubmitted: false));
            }

            // 6. Validate submission: câu hỏi phải thuộc quiz, đáp án phải thuộc câu hỏi.
            var selections = new Dictionary<Guid, Guid?>();
            foreach (var answer in request.Answers)
            {
                if (!questionsById.TryGetValue(answer.QuestionId, out var question))
                {
                    return ResponseDto<QuizResultDto>.FailResult(
                        "INVALID_SUBMISSION", "Bài nộp chứa câu hỏi không thuộc bài kiểm tra này.");
                }

                if (answer.SelectedOptionId.HasValue
                    && question.AnswerOptions.All(o => o.Id != answer.SelectedOptionId.Value))
                {
                    return ResponseDto<QuizResultDto>.FailResult(
                        "INVALID_SUBMISSION", "Bài nộp chứa đáp án không hợp lệ cho câu hỏi.");
                }

                // Giữ lựa chọn cuối cùng nếu client gửi trùng câu hỏi.
                selections[answer.QuestionId] = answer.SelectedOptionId;
            }

            // 7. Mốc hết giờ do server quyết định (AF-01): xác định auto-submit để ghi log.
            var deadline = attempt.StartedAt.AddMinutes(quiz.TimeLimitMinutes);
            var wasAutoSubmitted = DateTime.UtcNow > deadline;

            // 8. Chấm điểm phía server + lưu kết quả và cập nhật tiến độ trong CÙNG một giao dịch (AC-3).
            await _uow.BeginTransactionAsync(cancellationToken);
            try
            {
                foreach (var (questionId, optionId) in selections)
                {
                    if (optionId.HasValue)
                    {
                        var answer = new QuizAnswer(attempt.Id, questionId, optionId);
                        attempt.AddAnswer(answer); // giữ bất biến domain (1 đáp án / câu hỏi)
                        // Thêm tường minh vào DbSet để EF INSERT (QuizAnswer dùng client-generated Id).
                        await _uow.Quizzes.AddAnswerAsync(answer, cancellationToken);
                    }
                }

                var correctCount = CountCorrect(questionsById, selections);
                var totalQuestions = quiz.Questions.Count;

                // Điểm chuẩn hóa 0.00 - 100.00 (BR-02), kiểu decimal, 2 chữ số thập phân.
                var score = Math.Round(
                    (decimal)correctCount / totalQuestions * 100m, 2, MidpointRounding.AwayFromZero);
                var isPassed = score >= quiz.PassingScore;

                // attempt được nạp ở chế độ tracking nên thay đổi (Score/Status) và các
                // QuizAnswer mới thêm sẽ được EF lưu khi commit — không cần gọi Update tường minh.
                attempt.Submit(score, isPassed);

                // Cập nhật tiến độ khóa học (idempotent), giống luồng đánh dấu hoàn thành học liệu.
                var progress = await _uow.LearningProgresses.GetByCompositeKeyAsync(
                    enrollment.Id, request.MaterialId, cancellationToken);
                if (progress == null)
                {
                    progress = new LearningProgress(enrollment.Id, request.MaterialId);
                    await _uow.LearningProgresses.AddAsync(progress, cancellationToken);
                }

                if (!progress.IsCompleted)
                {
                    progress.Complete();
                    enrollment.CompleteMaterial();
                    _uow.Enrollments.Update(enrollment);
                }

                await _uow.CommitTransactionAsync(cancellationToken);

                _logger.LogInformation(
                    "QuizAttempt {AttemptId} đã nộp{Auto}: score={Score}, passed={Passed}, progress={Progress}%.",
                    attempt.Id, wasAutoSubmitted ? " (tự động do hết giờ)" : string.Empty,
                    score, isPassed, enrollment.ProgressPct);

                return ResponseDto<QuizResultDto>.SuccessResult(
                    BuildResult(attempt, quiz, enrollment, correctCount, wasAutoSubmitted));
            }
            catch (Exception)
            {
                await _uow.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }

        /// <summary>
        /// Chấm điểm: mỗi câu hỏi chỉ chọn một đáp án, đúng khi đáp án được chọn là đáp án đúng.
        /// </summary>
        private static int CountCorrect(
            IReadOnlyDictionary<Guid, Question> questionsById,
            IReadOnlyDictionary<Guid, Guid?> selections)
        {
            var correct = 0;
            foreach (var question in questionsById.Values)
            {
                if (!selections.TryGetValue(question.Id, out var selectedOptionId) || selectedOptionId is null)
                    continue;

                if (question.AnswerOptions.Any(o => o.Id == selectedOptionId.Value && o.IsCorrect))
                    correct++;
            }

            return correct;
        }

        private static QuizResultDto BuildResult(
            QuizAttempt attempt,
            QuizMaterial quiz,
            Enrollment enrollment,
            int correctCount,
            bool wasAutoSubmitted)
        {
            return new QuizResultDto
            {
                AttemptId = attempt.Id,
                Score = attempt.Score,
                IsPassed = attempt.IsPassed,
                PassingScore = quiz.PassingScore,
                TotalQuestions = quiz.Questions.Count,
                CorrectAnswers = correctCount,
                SubmittedAt = attempt.SubmittedAt,
                WasAutoSubmitted = wasAutoSubmitted,
                ProgressPct = enrollment.ProgressPct
            };
        }
    }
}
