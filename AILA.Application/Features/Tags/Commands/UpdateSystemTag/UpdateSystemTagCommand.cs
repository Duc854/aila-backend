using System;
using AILA.Application.Features.Tags.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Tags.Commands.UpdateSystemTag
{
    public record UpdateSystemTagCommand(
        Guid TagId,
        string Name,
        string Code
    ) : IRequest<ResponseDto<TagDto>>;
}