using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AILA.Domain.Enums;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Tags.Commands.ReviewTagVerifications
{
    public record ReviewTagVerificationCommand(
        Guid TagId,
        TagPublishRequestStatus Status,
        string? Note = null
    ) : IRequest<ResponseDto<bool>>;
}