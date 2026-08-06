using AILA.Application.Common.Dtos.AI;
using AILA.Application.Common.Interfaces.Repositories;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AILA.Application.Features.Quota.Queries.GetAdminUserTokenLogs;

public record GetAdminUserTokenLogsQuery(Guid AccountId) : IRequest<List<AITokenLogDto>>;

public class GetAdminUserTokenLogsQueryHandler : IRequestHandler<GetAdminUserTokenLogsQuery, List<AITokenLogDto>>
{
    private readonly IAccountResourceRepository _repository;

    public GetAdminUserTokenLogsQueryHandler(IAccountResourceRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<AITokenLogDto>> Handle(GetAdminUserTokenLogsQuery request, CancellationToken cancellationToken)
    {
        var logs = await _repository.GetTokenLogsForAccountAsync(request.AccountId, cancellationToken);
        return logs.Select(x => new AITokenLogDto
        {
            Id = x.Id,
            AccountId = x.AccountId,
            AttemptId = x.AttemptId,
            ServiceType = x.ServiceType,
            ModelId = x.ModelId,
            PromptTokens = x.PromptTokens,
            CompletionTokens = x.CompletionTokens,
            TotalTokens = x.TotalTokens,
            CreatedAt = x.CreatedAt
        }).ToList();
    }
}
