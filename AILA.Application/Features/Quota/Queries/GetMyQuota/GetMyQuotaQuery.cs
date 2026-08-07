using AILA.Application.Common.Dtos.AI;
using AILA.Application.Common.Interfaces.Repositories;
using AILA.Domain.Entities;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AILA.Application.Features.Quota.Queries.GetMyQuota;

public record GetMyQuotaQuery(Guid AccountId) : IRequest<UserQuotaStatusDto>;

public class GetMyQuotaQueryHandler : IRequestHandler<GetMyQuotaQuery, UserQuotaStatusDto>
{
    private readonly IAccountResourceRepository _repository;

    public GetMyQuotaQueryHandler(IAccountResourceRepository repository)
    {
        _repository = repository;
    }

    public async Task<UserQuotaStatusDto> Handle(GetMyQuotaQuery request, CancellationToken cancellationToken)
    {
        var quota = await _repository.GetQuotaAsync(request.AccountId, cancellationToken);

        int dailyLimit = quota?.DailyLimit ?? 50000;
        int usedToday = quota?.UsedAmountToday ?? 0;
        int remaining = Math.Max(0, dailyLimit - usedToday);
        int percentage = (int)Math.Min(100, Math.Round((double)usedToday / dailyLimit * 100));

        bool isExceeded = usedToday >= dailyLimit;
        bool isNearLimit = percentage >= 80;

        string message = isExceeded
            ? "Bạn đã sử dụng hết hạn mức Token hôm nay."
            : isNearLimit
                ? $"⚠️ Cảnh báo: Bạn đã sử dụng {percentage}% hạn mức Token hôm nay."
                : "Hạn mức Token bình thường.";

        return new UserQuotaStatusDto
        {
            AccountId = request.AccountId,
            DailyLimit = dailyLimit,
            MonthlyLimit = dailyLimit * 30,
            UsedToday = usedToday,
            RemainingToday = remaining,
            PercentageUsed = percentage,
            IsNearLimit = isNearLimit,
            IsExceeded = isExceeded,
            StatusMessage = message
        };
    }
}
