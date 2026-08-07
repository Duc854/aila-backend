using MediatR;
using Shared.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Features.ResourceLimitOverrides.Queries.GetAccountResourceLimitOverride
{
    public record GetAccountResourceLimitOverrideQuery(
        Guid AccountId
    ) : IRequest<ResponseDto<AccountResourceLimitOverrideDto>>;
}
