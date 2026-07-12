using System;
using AILA.Application.Features.Users.Dtos;
using AILA.Domain.Enums;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Users.Commands.UpdateUserRole
{
    public record UpdateUserRoleCommand(
        Guid UserId,
        UserRole Role
    ) : IRequest<ResponseDto<UserDetailDto>>;
}