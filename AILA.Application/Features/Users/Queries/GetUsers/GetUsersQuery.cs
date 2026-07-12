using System.Collections.Generic;
using AILA.Application.Features.Users.Dtos;
using AILA.Domain.Enums;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Users.Queries.GetUsers
{
    public record GetUsersQuery(
        string? SearchKeyword = null,
        UserRole? Role = null,
        bool? IsActive = null
    ) : IRequest<ResponseDto<List<UserListDto>>>;
}