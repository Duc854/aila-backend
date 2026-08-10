using System.Collections.Generic;
using AILA.Application.Features.Users.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Users.Queries.GetRoles
{
    public record GetRolesQuery() : IRequest<ResponseDto<List<RoleDto>>>;
}
