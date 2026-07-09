using AILA.Application.Common.Interfaces;
using AILA.Application.Features.Profile.Dtos;
using AILA.Application.Features.Profile.Mappers;
using AILA.Domain.Enums;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Profile.Queries.GetLearnerProfile
{
    public class GetLearnerProfileQueryHandler(IUnitOfWork uow)
        : IRequestHandler<GetLearnerProfileQuery, ResponseDto<LearnerProfileDto>>
    {
        /// <summary>Số mục hiển thị tối đa cho mỗi khối lịch sử trên trang Profile (UC-30).</summary>
        private const int PreviewCount = 5;

        public async Task<ResponseDto<LearnerProfileDto>> Handle(GetLearnerProfileQuery request, CancellationToken ct)
        {
            var learner = await uow.Learners.GetReadonlyWithUserAndGoalsAsync(request.UserId, ct);

            if (learner == null)
                return ResponseDto<LearnerProfileDto>.FailResult("LEARNER_NOT_FOUND", "Không tìm thấy thông tin học viên.");

            if (!learner.User.IsActive)
                return ResponseDto<LearnerProfileDto>.FailResult("ACCOUNT_INACTIVE", "Tài khoản đã bị vô hiệu hóa.");

            // Ownership (BR-01): mọi truy vấn đều lọc theo learner đang đăng nhập.
            var enrollments = await uow.Enrollments.GetEnrollmentsWithCourseByLearnerIdAsync(request.UserId, ct);

            // UC-30, AC-4: lịch sử quiz đã làm (mọi khóa học), đọc nguyên trạng dữ liệu đã lưu (BR-02).
            var attempts = await uow.Quizzes.GetSubmittedAttemptsByLearnerAsync(request.UserId, ct);

            // UC-30, AC-2: thống kê tóm tắt tính trên TOÀN BỘ dữ liệu (không phải chỉ 5 mục preview).
            // AverageQuizScore = null khi chưa có quiz nào (tránh chia cho 0).
            var summary = new LearningSummaryDto(
                TotalCourses: enrollments.Count,
                CoursesInProgress: enrollments.Count(e => e.Status == EnrollmentStatus.Active),
                CoursesCompleted: enrollments.Count(e => e.Status == EnrollmentStatus.Completed),
                TotalQuizzesTaken: attempts.Count,
                QuizzesPassed: attempts.Count(a => a.IsPassed),
                AverageQuizScore: attempts.Count > 0
                    ? Math.Round(attempts.Average(a => a.Score), 2)
                    : null
            );

            // Chỉ preview 5 mục mới nhất mỗi khối; FE dùng Summary.Total* để quyết định hiện nút "Xem tất cả".
            var recentEnrollments = enrollments.Take(PreviewCount).Select(e => e.ToSummaryDto());
            var recentQuizHistory = attempts.Take(PreviewCount).Select(a => a.ToHistoryDto());

            // UC-30, AC-5: lịch sử AI scenario. Luồng AI practice chưa lưu bản ghi nào ở tầng domain,
            // nên nguồn dữ liệu upstream đang rỗng → trả về danh sách rỗng (khối hiển thị empty, không lỗi).
            var recentAiScenarios = Enumerable.Empty<AiScenarioHistoryItemDto>();

            var dto = new LearnerProfileDto(
                learner.User.Id,
                learner.User.FullName,
                learner.User.Email,
                learner.User.AvatarUrl,
                learner.User.Role.ToString(),
                new LearnerInfoDto(
                    learner.LearnerType?.ToString(),
                    learner.KnowledgeLevel?.ToString(),
                    learner.HasCompletedOnboarding,
                    learner.LearningGoals.Select(t => new TagDto(t.Id, t.Name))
                ),
                recentEnrollments,
                summary,
                recentQuizHistory,
                recentAiScenarios
            );

            return ResponseDto<LearnerProfileDto>.SuccessResult(dto);
        }
    }
}
