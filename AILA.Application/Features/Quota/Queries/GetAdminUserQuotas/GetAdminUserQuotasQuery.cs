using AILA.Application.Common.Dtos.AI;
using AILA.Application.Common.Interfaces.Repositories;
using MediatR;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AILA.Application.Features.Quota.Queries.GetAdminUserQuotas;

public record GetAdminUserQuotasQuery() : IRequest<List<UserQuotaStatusDto>>;

public class GetAdminUserQuotasQueryHandler : IRequestHandler<GetAdminUserQuotasQuery, List<UserQuotaStatusDto>>
{
    private readonly IAccountResourceRepository _repository;

    public GetAdminUserQuotasQueryHandler(IAccountResourceRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<UserQuotaStatusDto>> Handle(GetAdminUserQuotasQuery request, CancellationToken cancellationToken)
    {
        var dummyAccountId = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6");
        var quota = await _repository.GetQuotaAsync(dummyAccountId, cancellationToken);

        int dailyLimit = quota?.DailyLimit ?? 50000;
        int usedToday = quota?.UsedAmountToday ?? 0;
        int remaining = Math.Max(0, dailyLimit - usedToday);
        int percentage = (int)Math.Min(100, Math.Round((double)usedToday / dailyLimit * 100));

        var result = new List<UserQuotaStatusDto>
        {
            new UserQuotaStatusDto
            {
                AccountId = dummyAccountId,
                DailyLimit = dailyLimit,
                MonthlyLimit = dailyLimit * 30,
                UsedToday = usedToday,
                RemainingToday = remaining,
                PercentageUsed = percentage,
                IsNearLimit = percentage >= 80,
                IsExceeded = usedToday >= dailyLimit,
                StatusMessage = usedToday >= dailyLimit ? "Đã vượt hạn mức" : "Bình thường"
            }
        };

        return result;
    }
}
