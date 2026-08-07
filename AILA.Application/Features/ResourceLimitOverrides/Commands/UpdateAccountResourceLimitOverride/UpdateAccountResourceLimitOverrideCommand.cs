using MediatR;
using Shared.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Features.ResourceLimitOverrides.Commands.UpdateAccountResourceLimitOverride
{
    public sealed record UpdateAccountResourceLimitOverrideCommand(
        Guid AdminId,
        Guid AccountId,
        int? AiTokenLimit,
        int? AiPracticeScenarioLimit,
        int? ExpertEvaluationRequestLimit
    ) : IRequest<ResponseDto<string>>;
}
