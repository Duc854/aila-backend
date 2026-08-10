using AILA.Application.Common.Dtos.AI;
using AILA.Application.Common.Interfaces.AI;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AILA.Application.Features.Quota.Queries.GetMyQuota;

public record GetMyQuotaQuery(Guid AccountId) : IRequest<UserQuotaStatusDto>;

public class GetMyQuotaQueryHandler : IRequestHandler<GetMyQuotaQuery, UserQuotaStatusDto>
{
    private readonly IQuotaService _quotaService;

    public GetMyQuotaQueryHandler(IQuotaService quotaService)
    {
        _quotaService = quotaService;
    }

    public async Task<UserQuotaStatusDto> Handle(GetMyQuotaQuery request, CancellationToken cancellationToken)
    {
        var checkResult = await _quotaService.CheckQuotaAsync(request.AccountId, 0, 0.80f, cancellationToken);

        return new UserQuotaStatusDto
        {
            AccountId = request.AccountId,
            DailyLimit = checkResult.DailyLimit,
            MonthlyLimit = checkResult.DailyLimit * 30,
            UsedToday = checkResult.UsedAmount,
            RemainingToday = checkResult.RemainingTokens,
            PercentageUsed = checkResult.PercentageUsed,
            IsNearLimit = checkResult.IsNearLimit,
            IsExceeded = !checkResult.IsAllowed,
            StatusMessage = checkResult.WarningMessage ?? "Hạn mức Token bình thường."
        };
    }
}
