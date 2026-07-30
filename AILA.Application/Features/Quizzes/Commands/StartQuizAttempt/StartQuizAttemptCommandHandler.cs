using AILA.Application.Common.Interfaces;
using AILA.Application.Features.Quizzes.Dtos;
using AILA.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Wrappers;

namespace AILA.Application.Features.Quizzes.Commands.StartQuizAttempt
{
    public class StartQuizAttemptCommandHandler
        : IRequestHandler<StartQuizAttemptCommand, ResponseDto<StartQuizResponseDto>>
    {
        private readonly IUnitOfWork _uow;
        private readonly ILogger<StartQuizAttemptCommandHandler> _logger;

        public StartQuizAttemptCommandHandler(
            IUnitOfWork uow,
            ILogger<StartQuizAttemptCommandHandler> logger)
        {
            _uow = uow;
            _logger = logger;
        }

        public async Task<ResponseDto<StartQuizResponseDto>> Handle(
            StartQuizAttemptCommand request, CancellationToken cancellationToken)
        {
            // 1. Kiểm soát quyền: chỉ Learner đã enroll mới được bắt đầu (BR-01, AC-7).
            var enrollment = await _uow.Enrollments.GetByCourseAndLearnerAsync(
                request.CourseId, request.LearnerId, cancellationToken);
            if (enrollment == null)
            {
                return ResponseDto<StartQuizResponseDto>.FailResult(
                    "ENROLLMENT_NOT_FOUND", "Bạn chưa đăng ký tham gia khóa học này.");
            }

            // 2. Nạp bài kiểm tra kèm câu hỏi + đáp án, đảm bảo thuộc đúng khóa học.
            var quiz = await _uow.Quizzes.GetQuizForLearningAsync(
                request.CourseId, request.MaterialId, cancellationToken);
            if (quiz == null)
            {
                return ResponseDto<StartQuizResponseDto>.FailResult(
                    "QUIZ_NOT_FOUND", "Không tìm thấy bài kiểm tra trong khóa học này.");
            }

            // 3. Thiếu cấu hình / đáp án (AF-02): không tạo attempt, báo lỗi rõ ràng.
            if (!quiz.Questions.Any() || quiz.Questions.Any(q => !q.AnswerOptions.Any()))
            {
                _logger.LogWarning(
                    "Quiz {MaterialId} thiếu cấu hình câu hỏi/đáp án, không thể bắt đầu.", request.MaterialId);
                return ResponseDto<StartQuizResponseDto>.FailResult(
                    "QUIZ_NOT_CONFIGURED", "Bài kiểm tra chưa được cấu hình đầy đủ câu hỏi hoặc đáp án.");
            }

            // 4. Idempotent: chỉ TIẾP TỤC lượt đang dở khi nó CHƯA hết giờ
            //    (refresh/đóng tab giữa chừng). Nếu lượt gần nhất đã hết hạn (hoặc chưa có),
            //    "Làm lại" phải mở lượt MỚI với deadline tương lai — không trả deadline quá khứ.
            var attempt = await _uow.Quizzes.GetInProgressAttemptAsync(
                enrollment.Id, request.MaterialId, cancellationToken);

            var now = DateTime.UtcNow;
            var isExpired = attempt != null
                && attempt.StartedAt.AddMinutes(quiz.TimeLimitMinutes) <= now;

            if (attempt == null || isExpired)
            {
                if (isExpired)
                {
                    _logger.LogInformation(
                        "QuizAttempt {AttemptId} đã hết giờ (bỏ dở), mở lượt làm mới.", attempt!.Id);
                }

                attempt = new QuizAttempt(enrollment.Id, request.MaterialId);
                await _uow.Quizzes.AddAttemptAsync(attempt, cancellationToken);
                await _uow.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Đã tạo QuizAttempt {AttemptId} cho Enrollment {EnrollmentId}, Quiz {MaterialId}.",
                    attempt.Id, enrollment.Id, request.MaterialId);
            }

            var dto = new StartQuizResponseDto
            {
                AttemptId = attempt.Id,
                MaterialId = request.MaterialId,
                TimeLimitMinutes = quiz.TimeLimitMinutes,
                PassingScore = quiz.PassingScore,
                ShowCorrectAnswersAfterSubmission = quiz.ShowCorrectAnswersAfterSubmission,
                StartedAt = attempt.StartedAt,
                DeadlineAt = attempt.StartedAt.AddMinutes(quiz.TimeLimitMinutes),
                Questions = quiz.Questions
                    .OrderBy(q => q.OrderIndex)
                    .Select(q => new QuizQuestionDto
                    {
                        QuestionId = q.Id,
                        Content = q.Content,
                        QuestionType = q.QuestionType.ToString(),
                        OrderIndex = q.OrderIndex,
                        Options = q.AnswerOptions
                            .OrderBy(o => o.OrderIndex)
                            .Select(o => new QuizOptionDto
                            {
                                OptionId = o.Id,
                                Content = o.Content,
                                OrderIndex = o.OrderIndex
                            })
                            .ToList()
                    })
                    .ToList()
            };

            return ResponseDto<StartQuizResponseDto>.SuccessResult(dto);
        }
    }
}
