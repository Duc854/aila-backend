using AILA.Application.Features.ResourceLimitOverrides.Queries.GetAccountResourceLimitOverride;
using MediatR;
using Shared.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Features.ResourceLimitOverrides.Commands.CreateAccountResourceLimitOverride
{
    public sealed record CreateAccountResourceLimitOverrideCommand(
        Guid AdminId,
        Guid AccountId,
        int? AiTokenLimit,
        int? AiPracticeScenarioLimit,
        int? ExpertEvaluationRequestLimit
    ) : IRequest<ResponseDto<string>>;
}
