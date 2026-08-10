using MediatR;
using Shared.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Features.ResourceLimitOverrides.Commands.DeleteAccountResourceLimitOverride
{
    public sealed record DeleteAccountResourceLimitOverrideCommand(
        Guid AdminId,
        Guid AccountId
    ) : IRequest<ResponseDto<string>>;
}
