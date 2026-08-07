using MediatR;
using Shared.Wrappers;
using AILA.Application.Common.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Features.ResourceLimitPolicy.Queries.GetDefaultResourceLimitPolicies
{
    public sealed record GetDefaultResourceLimitPoliciesQuery
        : IRequest<ResponseDto<List<ResourceLimitPolicyDto>>>;
}
