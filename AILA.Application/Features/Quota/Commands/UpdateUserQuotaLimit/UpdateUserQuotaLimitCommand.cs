using AILA.Application.Common.Interfaces;
using AILA.Application.Common.Dtos.AI;
using AILA.Application.Common.Interfaces.Repositories;
using AILA.Domain.Entities;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AILA.Application.Features.Quota.Commands.UpdateUserQuotaLimit;

public record UpdateUserQuotaLimitCommand(Guid AccountId, int DailyLimit, int MonthlyLimit) : IRequest<UserQuotaStatusDto>;

public class UpdateUserQuotaLimitCommandHandler : IRequestHandler<UpdateUserQuotaLimitCommand, UserQuotaStatusDto>
{
    private readonly IAccountResourceRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateUserQuotaLimitCommandHandler(
        IAccountResourceRepository repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<UserQuotaStatusDto> Handle(UpdateUserQuotaLimitCommand request, CancellationToken cancellationToken)
    {
        var quota = await _repository.GetQuotaAsync(request.AccountId, cancellationToken);
        if (quota == null)
        {
            quota = new UserTokenQuota(request.AccountId, request.DailyLimit);
            await _repository.AddQuotaAsync(quota, cancellationToken);
        }
        else
        {
            quota.UpdateDailyLimit(request.DailyLimit);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        int usedToday = quota.UsedAmountToday;
        int remaining = Math.Max(0, request.DailyLimit - usedToday);
        int percentage = (int)Math.Min(100, Math.Round((double)usedToday / request.DailyLimit * 100));

        return new UserQuotaStatusDto
        {
            AccountId = request.AccountId,
            DailyLimit = request.DailyLimit,
            MonthlyLimit = request.MonthlyLimit,
            UsedToday = usedToday,
            RemainingToday = remaining,
            PercentageUsed = percentage,
            IsNearLimit = percentage >= 80,
            IsExceeded = usedToday >= request.DailyLimit,
            StatusMessage = "Cập nhật hạn mức Token thành công."
        };
    }
}
