using System;
using AILA.Application.Features.Users.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Users.Commands.UpdateUserStatus
{
    public record UpdateUserStatusCommand(
        Guid UserId,
        bool IsActive
    ) : IRequest<ResponseDto<UserDetailDto>>;
}
