using AILA.Application.Common.Exceptions;
using AILA.Application.Common.Interfaces;
using AILA.Application.Common.Interfaces.Repositories;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AILA.Application.Features.PracticeAttempts.Commands.CreateAttempt;

public class CreateAttemptCommandHandler : IRequestHandler<CreateAttemptCommand, Guid>
{
    private readonly IPracticeAttemptRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateAttemptCommandHandler(IPracticeAttemptRepository repository, IUnitOfWork unitOfWork) {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateAttemptCommand request, CancellationToken cancellationToken) {
        var enrollment = await _unitOfWork.Enrollments.GetByIdAsync(request.EnrollmentId);
        if (enrollment == null)
        {
            throw new NotFoundException(nameof(Enrollment), request.EnrollmentId);
        }

        var aiPractice = await _unitOfWork.AIPracticeMaterials.GetByIdAsync(request.MaterialId);
        if (aiPractice == null)
        {
            throw new NotFoundException("Bài thực hành AI (AIPracticeMaterial) không tồn tại hoặc chưa được thiết lập kịch bản", request.MaterialId);
        }

        // 1. Kiểm tra và trừ định ngạch kịch bản thực hành AI (AiPracticeScenarioLimit)
        var quota = await ResolveQuotaAsync(enrollment.LearnerId, cancellationToken);
        if (quota.Usage.AiPracticeScenarioUsed >= quota.Limit)
        {
            throw new InvalidOperationException(
                $"Bạn đã sử dụng hết định ngạch thực hành kịch bản AI ({quota.Usage.AiPracticeScenarioUsed}/{quota.Limit} lượt). Vui lòng nâng cấp gói đăng ký để tiếp tục.");
        }

        quota.Usage.ConsumeAiPracticeScenario(1);

        if (quota.IsNewPeriod)
            await _unitOfWork.AccountResourceUsages.AddAsync(quota.Usage);
        else
            _unitOfWork.AccountResourceUsages.Update(quota.Usage);

        // 2. Ghi nhận truy cập học liệu để lưu vị trí học gần nhất của học viên
        var progress = await _unitOfWork.LearningProgresses.GetByCompositeKeyAsync(request.EnrollmentId, request.MaterialId, cancellationToken);
        if (progress == null)
        {
            progress = new LearningProgress(request.EnrollmentId, request.MaterialId);
            await _unitOfWork.LearningProgresses.AddAsync(progress, cancellationToken);
        }
        else
        {
            progress.TrackAccess();
        }

        // 3. Khởi tạo và lưu phiên luyện tập mới
        var attempt = new PracticeAttempt(request.EnrollmentId, request.MaterialId);
        await _repository.AddAsync(attempt, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return attempt.Id;
    }

    /// <summary>
    /// Định ngạch ưu tiên theo thứ tự: ghi đè riêng tài khoản → gói đang dùng → chính sách mặc định do Admin cấu hình.
    /// Kỳ sử dụng hết hạn (hoặc chưa có) thì mở kỳ mới để bộ đếm không bị cộng dồn vô hạn.
    /// </summary>
    private async Task<QuotaSnapshot> ResolveQuotaAsync(Guid learnerId, CancellationToken ct)
    {
        var subscription = await _unitOfWork.Subscriptions
            .GetActiveSubscriptionByLearnerIdToCalculateResourceAsync(learnerId, ct);

        // Hệ thống không có background job nên gói được cho hết hạn ngay tại lúc dùng tài nguyên.
        // Lưu ngay để trạng thái không bị mất khi luồng phía sau dừng vì hết định ngạch.
        if (subscription is not null && subscription.IsExpired())
        {
            subscription.Expire();
            await _unitOfWork.SaveChangesAsync(ct);
            subscription = null;
        }

        var accountLimit = await _unitOfWork.AccountResourceLimits
            .GetByAccountIdAsync(learnerId, ct);

        var defaultPolicy = await _unitOfWork.ResourceLimitPolicies
            .GetByAccountTypeAsync(ResourceAccountType.Learner, ct);

        var limit = accountLimit?.AiPracticeScenarioLimit
            ?? subscription?.PlanSnapshot?.AiPracticeScenarioLimit
            ?? defaultPolicy?.AiPracticeScenarioLimit
            ?? 0;

        var now = DateTime.UtcNow;
        var usage = await _unitOfWork.AccountResourceUsages.GetByAccountIdAsync(learnerId, ct);

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

    private sealed record QuotaSnapshot(
        int Limit,
        AccountResourceUsage Usage,
        bool IsNewPeriod);
}

