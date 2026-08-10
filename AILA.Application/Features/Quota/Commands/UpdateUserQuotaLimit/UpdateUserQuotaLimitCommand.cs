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
        var accountOverride = await _unitOfWork.AccountResourceLimits.GetByAccountIdAsync(request.AccountId, cancellationToken);
        if (accountOverride == null)
        {
            accountOverride = new AccountResourceLimit(request.AccountId, aiTokenLimit: request.DailyLimit);
            await _unitOfWork.AccountResourceLimits.AddAsync(accountOverride);
        }
        else
        {
            accountOverride.UpdateLimits(request.DailyLimit, accountOverride.AiPracticeScenarioLimit, accountOverride.ExpertEvaluationRequestLimit);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        int usedToday = await _repository.GetTodayTokenUsageAsync(request.AccountId, cancellationToken);
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
