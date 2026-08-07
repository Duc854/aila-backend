using AILA.Application.Common.Dtos.AI;
using AILA.Application.Common.Interfaces.AI;
using MediatR;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AILA.Application.Features.Quota.Queries.GetAdminUserQuotas;

public record GetAdminUserQuotasQuery() : IRequest<List<UserQuotaStatusDto>>;

public class GetAdminUserQuotasQueryHandler : IRequestHandler<GetAdminUserQuotasQuery, List<UserQuotaStatusDto>>
{
    private readonly IQuotaService _quotaService;

    public GetAdminUserQuotasQueryHandler(IQuotaService quotaService)
    {
        _quotaService = quotaService;
    }

    public async Task<List<UserQuotaStatusDto>> Handle(GetAdminUserQuotasQuery request, CancellationToken cancellationToken)
    {
        var dummyAccountId = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6");
        var checkResult = await _quotaService.CheckQuotaAsync(dummyAccountId, 0, 0.80f, cancellationToken);

        var result = new List<UserQuotaStatusDto>
        {
            new UserQuotaStatusDto
            {
                AccountId = dummyAccountId,
                DailyLimit = checkResult.DailyLimit,
                MonthlyLimit = checkResult.DailyLimit * 30,
                UsedToday = checkResult.UsedAmount,
                RemainingToday = checkResult.RemainingTokens,
                PercentageUsed = checkResult.PercentageUsed,
                IsNearLimit = checkResult.IsNearLimit,
                IsExceeded = !checkResult.IsAllowed,
                StatusMessage = !checkResult.IsAllowed ? "Đã vượt hạn mức" : "Bình thường"
            }
        };

        return result;
    }
}
