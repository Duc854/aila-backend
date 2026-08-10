using AILA.Application.Common.Dtos;
using MediatR;
using Shared.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Features.ResourceLimitOverrides.Queries.GetOverrideEligibleAccounts
{
    public record GetOverrideEligibleAccountsQuery(
        string? Keyword,
        PageRequest PageRequest
    ) : IRequest<ResponseDto<PageResult<AccountOverrideAccountDto>>>;
}
