using System;
using AILA.Application.Features.Users.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Users.Queries.GetUserById
{
    public record GetUserByIdQuery(
        Guid UserId
    ) : IRequest<ResponseDto<UserDetailDto>>;
}
