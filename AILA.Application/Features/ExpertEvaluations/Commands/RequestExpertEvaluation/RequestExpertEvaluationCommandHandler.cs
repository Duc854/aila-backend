using AILA.Application.Common.Interfaces;
using AILA.Application.Common.Notifications;
using AILA.Application.Features.ExpertEvaluations.Dtos;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Wrappers;

namespace AILA.Application.Features.ExpertEvaluations.Commands.RequestExpertEvaluation
{
    public sealed class RequestExpertEvaluationCommandHandler
        : IRequestHandler<RequestExpertEvaluationCommand, ResponseDto<ExpertEvaluationRequestCreatedDto>>
    {
        /// <summary>Định ngạch mặc định khi hệ thống chưa cấu hình chính sách nào.</summary>
        private const int FallbackQuotaLimit = 1;

        private readonly IUnitOfWork _uow;
        private readonly ILogger<RequestExpertEvaluationCommandHandler> _logger;

        public RequestExpertEvaluationCommandHandler(
            IUnitOfWork uow,
            ILogger<RequestExpertEvaluationCommandHandler> logger)
        {
            _uow = uow;
            _logger = logger;
        }

        public async Task<ResponseDto<ExpertEvaluationRequestCreatedDto>> Handle(
            RequestExpertEvaluationCommand request,
            CancellationToken ct)
        {
            // 1. Lượt thực hành phải tồn tại và thuộc về học viên đang đăng nhập (AF-04).
            //    Không phân biệt "không tồn tại" với "không thuộc quyền" để tránh dò ID.
            var attempt = await _uow.PracticeAttempts.GetByIdAsync(request.PracticeAttemptId, ct);
            if (attempt is null)
                return NotFound();

            var enrollment = await _uow.Enrollments.GetByIdAsync(attempt.EnrollmentId);
            if (enrollment is null || enrollment.LearnerId != request.LearnerId)
                return NotFound();

            // 2. BR-01: chỉ nhờ chuyên gia khi AI đã chấm xong (AF-01).
            if (attempt.Status != PracticeAttemptStatus.Completed || !attempt.FinalScore.HasValue)
                return ResponseDto<ExpertEvaluationRequestCreatedDto>.FailResult(
                    ExpertEvaluationErrors.AiEvaluationUnavailable,
                    "Lượt thực hành chưa có kết quả đánh giá của AI nên chưa thể nhờ chuyên gia.");

            // 3. BR-02: mỗi lượt thực hành chỉ được nhờ đánh giá một lần (AF-02).
            var alreadyRequested = await _uow.ExpertEvaluationRequests
                .HasActiveRequestForAttemptAsync(attempt.Id, ct);

            if (alreadyRequested)
                return ResponseDto<ExpertEvaluationRequestCreatedDto>.FailResult(
                    ExpertEvaluationErrors.EvaluationAlreadyRequested,
                    "Lượt thực hành này đã được gửi cho chuyên gia đánh giá.");

            // 4. BR-04: yêu cầu được giao cho chuyên gia tác giả khóa học.
            //    Tác giả bị vô hiệu hóa thì chặn ngay, không tạo yêu cầu treo (AC-29.7).
            var course = await _uow.Courses.GetByIdAsync(enrollment.CourseId);
            if (course is null)
                return ResponseDto<ExpertEvaluationRequestCreatedDto>.FailResult(
                    ExpertEvaluationErrors.ExpertUnavailable,
                    "Không tìm thấy khóa học của lượt thực hành này.");

            var authorAccount = await _uow.Users.GetByIdAsync(course.ExpertId);
            if (authorAccount is null || !authorAccount.IsActive)
                return ResponseDto<ExpertEvaluationRequestCreatedDto>.FailResult(
                    ExpertEvaluationErrors.ExpertUnavailable,
                    "Chuyên gia phụ trách khóa học hiện không khả dụng. Vui lòng thử lại sau.");

            // 5. BR-03: trừ đúng một lượt quota (AF-03).
            var quota = await ResolveQuotaAsync(request.LearnerId, ct);
            if (quota.Usage.ExpertEvaluationRequestUsed >= quota.Limit)
                return ResponseDto<ExpertEvaluationRequestCreatedDto>.FailResult(
                    ExpertEvaluationErrors.QuotaExhausted,
                    "Bạn đã dùng hết lượt nhờ chuyên gia đánh giá. Vui lòng nâng cấp gói để tiếp tục.");

            quota.Usage.ConsumeExpertEvaluationRequest();

            if (quota.IsNewPeriod)
                await _uow.AccountResourceUsages.AddAsync(quota.Usage);
            else
                _uow.AccountResourceUsages.Update(quota.Usage);

            // 6. Tạo yêu cầu rồi giao ngay cho tác giả khóa học.
            var evaluationRequest = new ExpertEvaluationRequest(attempt.Id, request.LearnerId);
            evaluationRequest.AssignExpert(course.ExpertId);

            await _uow.ExpertEvaluationRequests.AddAsync(evaluationRequest);

            // 7. Báo cho chuyên gia tác giả biết có việc mới. authorAccount đã được xác nhận
            //    tồn tại và còn hoạt động ở bước 4 nên không cần kiểm tra lại.
            await _uow.Notifications.AddAsync(
                NotificationTemplates.ExpertEvaluationRequested(
                    course.ExpertId,
                    evaluationRequest.Id,
                    course.Name));

            // Một lần SaveChanges duy nhất => tạo yêu cầu, trừ quota và phát thông báo nằm chung
            // một transaction, hỏng bước nào thì quota cũng không bị trừ (AC-29.6, E29-5).
            await _uow.SaveChangesAsync(ct);

            var remainingQuota = Math.Max(0, quota.Limit - quota.Usage.ExpertEvaluationRequestUsed);

            _logger.LogInformation(
                "Đã tạo yêu cầu đánh giá chuyên gia {RequestId} cho lượt thực hành {AttemptId} " +
                "của học viên {LearnerId}, giao cho chuyên gia {ExpertId}. Quota còn lại {RemainingQuota}/{QuotaLimit}.",
                evaluationRequest.Id,
                attempt.Id,
                request.LearnerId,
                course.ExpertId,
                remainingQuota,
                quota.Limit);

            return ResponseDto<ExpertEvaluationRequestCreatedDto>.SuccessResult(
                new ExpertEvaluationRequestCreatedDto
                {
                    RequestId = evaluationRequest.Id,
                    PracticeAttemptId = evaluationRequest.PracticeAttemptId,
                    ExpertId = evaluationRequest.ExpertId,
                    Status = evaluationRequest.Status.ToString(),
                    RequestedAt = evaluationRequest.RequestedAt,
                    RemainingQuota = remainingQuota
                });
        }

        /// <summary>
        /// Định ngạch ưu tiên theo thứ tự: ghi đè riêng tài khoản → gói đang dùng → chính sách mặc định.
        /// Kỳ sử dụng hết hạn (hoặc chưa có) thì mở kỳ mới để bộ đếm không bị cộng dồn vô hạn.
        /// </summary>
        private async Task<QuotaSnapshot> ResolveQuotaAsync(Guid learnerId, CancellationToken ct)
        {
            var subscription = await _uow.Subscriptions
                .GetActiveSubscriptionByLearnerIdAsync(learnerId, ct);

            var accountLimit = await _uow.AccountResourceLimits
                .GetByAccountIdAsync(learnerId, ct);

            var defaultPolicy = await _uow.ResourceLimitPolicies
                .GetByAccountTypeAsync(ResourceAccountType.Learner, ct);

            var limit = accountLimit?.ExpertEvaluationRequestLimit
                ?? subscription?.PlanSnapshot?.ExpertEvaluationLimit
                ?? defaultPolicy?.ExpertEvaluationRequestLimit
                ?? FallbackQuotaLimit;

            var now = DateTime.UtcNow;
            var usage = await _uow.AccountResourceUsages.GetByAccountIdAsync(learnerId, ct);

            if (usage is not null && !usage.IsExpired(now))
                return new QuotaSnapshot(limit, usage, IsNewPeriod: false);

            var (periodStart, periodEnd) = subscription is not null && subscription.ExpiredAt > now
                ? (subscription.ActivatedAt, subscription.ExpiredAt)
                : CurrentCalendarMonth(now);

            return new QuotaSnapshot(
                limit,
                new AccountResourceUsage(learnerId, periodStart, periodEnd),
                IsNewPeriod: true);
        }

        private static (DateTime Start, DateTime End) CurrentCalendarMonth(DateTime utcNow)
        {
            var start = new DateTime(utcNow.Year, utcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            return (start, start.AddMonths(1));
        }

        private static ResponseDto<ExpertEvaluationRequestCreatedDto> NotFound()
            => ResponseDto<ExpertEvaluationRequestCreatedDto>.FailResult(
                ExpertEvaluationErrors.PracticeAttemptNotFound,
                "Không tìm thấy lượt thực hành.");

        private sealed record QuotaSnapshot(
            int Limit,
            AccountResourceUsage Usage,
            bool IsNewPeriod);
    }
}
