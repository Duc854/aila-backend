using AILA.Application.Common.Interfaces;
using AILA.Application.Features.Quizzes.Dtos;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Wrappers;

namespace AILA.Application.Features.Quizzes.Queries.GetQuizResultDetail
{
    public class GetQuizResultDetailQueryHandler
        : IRequestHandler<GetQuizResultDetailQuery, ResponseDto<QuizResultDetailDto>>
    {
        private readonly IUnitOfWork _uow;
        private readonly ILogger<GetQuizResultDetailQueryHandler> _logger;

        public GetQuizResultDetailQueryHandler(
            IUnitOfWork uow,
            ILogger<GetQuizResultDetailQueryHandler> logger)
        {
            _uow = uow;
            _logger = logger;
        }

        public async Task<ResponseDto<QuizResultDetailDto>> Handle(
            GetQuizResultDetailQuery request, CancellationToken cancellationToken)
        {
            // Ownership (BR-03): chỉ tra theo enrollment của chính Learner đang đăng nhập.
            var enrollment = await _uow.Enrollments.GetByCourseAndLearnerAsync(
                request.CourseId, request.LearnerId, cancellationToken);
            if (enrollment == null)
            {
                return ResponseDto<QuizResultDetailDto>.FailResult(
                    "ENROLLMENT_NOT_FOUND", "Bạn chưa đăng ký tham gia khóa học này.");
            }

            var quiz = await _uow.Quizzes.GetQuizForLearningAsync(
                request.CourseId, request.MaterialId, cancellationToken);
            if (quiz == null)
            {
                return ResponseDto<QuizResultDetailDto>.FailResult(
                    "QUIZ_NOT_FOUND", "Không tìm thấy bài kiểm tra trong khóa học này.");
            }

            // AF-02 / AC-4: quiz cấu hình ẩn đáp án → không cho mở chi tiết (quyết định ở server, BR-01).
            if (!quiz.ShowCorrectAnswersAfterSubmission)
            {
                return ResponseDto<QuizResultDetailDto>.FailResult(
                    "ANSWERS_HIDDEN", "Bài kiểm tra này không cho phép xem lại đáp án.");
            }

            var attempt = await _uow.Quizzes.GetLatestSubmittedAttemptAsync(
                enrollment.Id, request.MaterialId, cancellationToken);
            if (attempt == null)
            {
                return ResponseDto<QuizResultDetailDto>.FailResult(
                    "NO_RESULT", "Bạn chưa có lượt làm bài nào đã hoàn thành.");
            }

            // Lựa chọn của Learner theo từng câu (dữ liệu đã đóng băng ở UC-26).
            var selectionByQuestion = attempt.Answers
                .GroupBy(a => a.QuestionId)
                .ToDictionary(g => g.Key, g => g.First().SelectedAnswerOptionId);

            var questions = quiz.Questions
                .OrderBy(q => q.OrderIndex)
                .Select(q =>
                {
                    selectionByQuestion.TryGetValue(q.Id, out var selectedOptionId);
                    return new QuizResultQuestionDto
                    {
                        QuestionId = q.Id,
                        Content = q.Content,
                        QuestionType = q.QuestionType.ToString(),
                        OrderIndex = q.OrderIndex,
                        SelectedOptionId = selectedOptionId,
                        IsCorrect = QuizGrading.IsSelectionCorrect(q, selectedOptionId),
                        Options = q.AnswerOptions
                            .OrderBy(o => o.OrderIndex)
                            .Select(o => new QuizResultOptionDto
                            {
                                OptionId = o.Id,
                                Content = o.Content,
                                OrderIndex = o.OrderIndex,
                                IsCorrect = o.IsCorrect,
                                IsSelected = selectedOptionId == o.Id
                            })
                            .ToList()
                    };
                })
                .ToList();

            // Audit truy cập chi tiết (NFR logging).
            _logger.LogInformation(
                "Learner {LearnerId} xem chi tiết kết quả QuizAttempt {AttemptId} (quiz {MaterialId}).",
                request.LearnerId, attempt.Id, request.MaterialId);

            var dto = new QuizResultDetailDto
            {
                AttemptId = attempt.Id,
                Score = attempt.Score,
                IsPassed = attempt.IsPassed,
                PassingScore = quiz.PassingScore,
                TotalQuestions = quiz.Questions.Count,
                CorrectAnswers = questions.Count(x => x.IsCorrect),
                SubmittedAt = attempt.SubmittedAt,
                Questions = questions
            };

            return ResponseDto<QuizResultDetailDto>.SuccessResult(dto);
        }
    }
}
