using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AILA.Application.Features.Users.Dtos;
using AILA.Domain.Enums;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Users.Queries.GetRoles
{
    public class GetRolesQueryHandler
        : IRequestHandler<GetRolesQuery, ResponseDto<List<RoleDto>>>
    {
        public Task<ResponseDto<List<RoleDto>>> Handle(
            GetRolesQuery request,
            CancellationToken cancellationToken)
        {
            var roles = new List<RoleDto>
            {
                new() { Value = UserRole.Expert, Name = "Expert" },
                new() { Value = UserRole.Learner, Name = "Learner" }
                // Không bao gồm Admin vì BR-04
            };

            return Task.FromResult(
                ResponseDto<List<RoleDto>>.SuccessResult(roles));
        }
    }
}