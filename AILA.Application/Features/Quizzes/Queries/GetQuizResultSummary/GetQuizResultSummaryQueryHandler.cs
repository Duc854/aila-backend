using AILA.Application.Common.Interfaces;
using AILA.Application.Features.Quizzes.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Quizzes.Queries.GetQuizResultSummary
{
    public class GetQuizResultSummaryQueryHandler
        : IRequestHandler<GetQuizResultSummaryQuery, ResponseDto<QuizResultSummaryDto>>
    {
        private readonly IUnitOfWork _uow;

        public GetQuizResultSummaryQueryHandler(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<ResponseDto<QuizResultSummaryDto>> Handle(
            GetQuizResultSummaryQuery request, CancellationToken cancellationToken)
        {
            // Ownership (BR-03): chỉ tra theo enrollment của chính Learner đang đăng nhập.
            var enrollment = await _uow.Enrollments.GetByCourseAndLearnerAsync(
                request.CourseId, request.LearnerId, cancellationToken);
            if (enrollment == null)
            {
                return ResponseDto<QuizResultSummaryDto>.FailResult(
                    "ENROLLMENT_NOT_FOUND", "Bạn chưa đăng ký tham gia khóa học này.");
            }

            var quiz = await _uow.Quizzes.GetQuizForLearningAsync(
                request.CourseId, request.MaterialId, cancellationToken);
            if (quiz == null)
            {
                return ResponseDto<QuizResultSummaryDto>.FailResult(
                    "QUIZ_NOT_FOUND", "Không tìm thấy bài kiểm tra trong khóa học này.");
            }

            var attempt = await _uow.Quizzes.GetLatestSubmittedAttemptAsync(
                enrollment.Id, request.MaterialId, cancellationToken);

            // AF-01 / AC-5: chưa có lượt làm nào đã nộp → empty state (không phải lỗi).
            if (attempt == null)
            {
                return ResponseDto<QuizResultSummaryDto>.SuccessResult(new QuizResultSummaryDto
                {
                    HasResult = false,
                    TotalQuestions = quiz.Questions.Count,
                    PassingScore = quiz.PassingScore,
                    CanViewDetails = quiz.ShowCorrectAnswersAfterSubmission
                });
            }

            // Đếm số câu đúng từ dữ liệu đã đóng băng (điểm số vẫn đọc nguyên như đã lưu).
            var questionsById = quiz.Questions.ToDictionary(q => q.Id);
            var selections = attempt.Answers
                .Where(a => a.SelectedAnswerOptionId.HasValue)
                .GroupBy(a => a.QuestionId)
                .ToDictionary(g => g.Key, g => g.Select(a => a.SelectedAnswerOptionId!.Value).ToList());
            var correctAnswers = QuizGrading.CountCorrect(questionsById, selections);

            var dto = new QuizResultSummaryDto
            {
                HasResult = true,
                AttemptId = attempt.Id,
                Score = attempt.Score,
                IsPassed = attempt.IsPassed,
                PassingScore = quiz.PassingScore,
                TotalQuestions = quiz.Questions.Count,
                CorrectAnswers = correctAnswers,
                StartedAt = attempt.StartedAt,
                SubmittedAt = attempt.SubmittedAt,
                CanViewDetails = quiz.ShowCorrectAnswersAfterSubmission
            };

            return ResponseDto<QuizResultSummaryDto>.SuccessResult(dto);
        }
    }
}
