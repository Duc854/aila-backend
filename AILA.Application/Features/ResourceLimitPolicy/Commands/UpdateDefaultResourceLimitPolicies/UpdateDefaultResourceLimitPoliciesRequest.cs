using AILA.Application.Common.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Features.ResourceLimitPolicy.Commands.UpdateDefaultResourceLimitPolicies
{
    public sealed class UpdateDefaultResourceLimitPoliciesRequest
    {
        public List<ResourceLimitPolicyDto> Policies { get; set; } = [];
    }
}
