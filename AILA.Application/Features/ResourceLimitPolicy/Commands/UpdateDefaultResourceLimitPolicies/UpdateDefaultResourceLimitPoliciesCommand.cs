using AILA.Application.Common.Dtos;
using MediatR;
using Shared.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Features.ResourceLimitPolicy.Commands.UpdateDefaultResourceLimitPolicies
{
    public sealed record UpdateDefaultResourceLimitPoliciesCommand(
        List<ResourceLimitPolicyDto> Policies,
        Guid AdminId
    ) : IRequest<ResponseDto<string>>;
}
